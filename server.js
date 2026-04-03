const express = require('express')
const http = require('http')
const https = require('https')
const WebSocket = require('ws')
const tls = require('tls')
const path = require('path')
const fs = require('fs')

let serverConfig = { twitch: { username: '', clientId: '', clientSecret: '' } }
try {
    serverConfig = require('./server-config')
} catch {
    // server-config.js not found — chat commands disabled
}

const TOKEN_FILE = path.join(__dirname, 'server-token.json')

function loadToken() {
    try {
        return JSON.parse(fs.readFileSync(TOKEN_FILE, 'utf8')).oauthToken || ''
    } catch {
        return ''
    }
}

function saveToken(token) {
    fs.writeFileSync(TOKEN_FILE, JSON.stringify({ oauthToken: token }, null, 2))
    console.log('[Auth] Token saved to server-token.json')
}

// ---------------------------------------------------------------------------
// Twitch IRC chat integration
// ---------------------------------------------------------------------------

let ircSocket = null
let ircChannel = null
let ircReady = false

function connectIRC(channel) {
    const { username } = serverConfig.twitch
    const token = loadToken()
    if (!username || !token) return

    // Only allow connecting to the token owner's own channel
    if (channel.toLowerCase() !== username.toLowerCase()) {
        console.log(`[IRC] Refusing to join #${channel} — only ${username}'s own channel is permitted`)
        return
    }

    if (ircSocket) {
        ircSocket.destroy()
        ircSocket = null
    }

    ircReady = false
    ircChannel = `#${channel.toLowerCase()}`

    ircSocket = tls.connect(6697, 'irc.chat.twitch.tv', { rejectUnauthorized: false }, () => {
        ircSocket.write(`PASS oauth:${token}\r\n`)
        ircSocket.write(`NICK ${username.toLowerCase()}\r\n`)
        ircSocket.write(`JOIN ${ircChannel}\r\n`)
    })

    ircSocket.on('data', (data) => {
        const str = data.toString()
        if (str.includes('PING')) {
            ircSocket.write('PONG :tmi.twitch.tv\r\n')
            console.log('[IRC] PING received — PONG sent')
        }
        if (!ircReady && (str.includes(' 001 ') || str.includes('366') || str.includes(`JOIN ${ircChannel}`))) {
            ircReady = true
            console.log(`[IRC] Joined ${ircChannel}`)
        }
        // Log NOTICE lines (auth failures, rate limits) and the server welcome line
        for (const line of str.split('\r\n')) {
            if (!line) continue
            if (line.includes(' NOTICE ')) console.log('[IRC]', line.trim())
            else if (line.includes(' 001 ')) console.log('[IRC] Connected to Twitch IRC', line.trim()) 
            else if (line.includes(' 002' ) || line.includes(' 003' ) || line.includes(' 372')) (console.log('[IRC]', line.trim()))
        }
    })

    ircSocket.on('error', (err) => console.error('[IRC] Error:', err.message))
    ircSocket.on('close', () => {
        ircReady = false
        ircSocket = null
        if (ircChannel) {
            console.log('[IRC] Disconnected — reconnecting in 10s')
            setTimeout(() => connectIRC(ircChannel.slice(1)), 10000)
        } else {
            console.log('[IRC] Disconnected')
        }
    })
}

function sendChat(message) {
    if (!ircSocket || !ircReady || !ircChannel) return
    ircSocket.write(`PRIVMSG ${ircChannel} :${message}\r\n`)
    console.log(`[IRC] Sent: ${message}`)
}

// ---------------------------------------------------------------------------
// HTTP helpers
// ---------------------------------------------------------------------------

function httpsPost(url, params) {
    return new Promise((resolve, reject) => {
        const body = new URLSearchParams(params).toString()
        const urlObj = new URL(url)
        const options = {
            hostname: urlObj.hostname,
            path: urlObj.pathname,
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'Content-Length': Buffer.byteLength(body)
            }
        }
        const req = https.request(options, (res) => {
            let data = ''
            res.on('data', chunk => data += chunk)
            res.on('end', () => {
                try { resolve(JSON.parse(data)) } catch { reject(new Error('Invalid JSON response')) }
            })
        })
        req.on('error', reject)
        req.write(body)
        req.end()
    })
}

// ---------------------------------------------------------------------------
// HTTP routes
// ---------------------------------------------------------------------------

const app = express()
const server = http.createServer(app)
const wss = new WebSocket.Server({ server })

// Serve Spinner static assets (css, scripts, config, etc.)
app.use(express.static(path.join(__dirname, 'Spinner')))

// Overlay — what goes in the OBS Browser Source (on stream)
app.get('/', (req, res) => {
    res.sendFile(path.join(__dirname, 'Spinner', 'overlay.html'))
})

// Control panel — what goes in the OBS Browser Dock
app.get('/control', (req, res) => {
    res.sendFile(path.join(__dirname, 'Spinner', 'SongSpinner.html'))
})

// Step 1: Redirect user to Twitch for authorization
app.get('/auth', (req, res) => {
    const { clientId } = serverConfig.twitch
    if (!clientId) {
        return res.send('<pre>clientId not set in server-config.js</pre>')
    }
    const params = new URLSearchParams({
        client_id: clientId,
        redirect_uri: `http://localhost:${PORT}/auth/callback`,
        response_type: 'code',
        scope: 'chat:read chat:edit'
    })
    res.redirect(`https://id.twitch.tv/oauth2/authorize?${params}`)
})

// Step 2: Twitch redirects back here with a code; exchange it for a token
app.get('/auth/callback', async (req, res) => {
    const { code } = req.query
    if (!code) return res.send('<pre>No authorization code received.</pre>')

    const { clientId, clientSecret } = serverConfig.twitch
    try {
        const data = await httpsPost('https://id.twitch.tv/oauth2/token', {
            client_id: clientId,
            client_secret: clientSecret,
            code,
            grant_type: 'authorization_code',
            redirect_uri: `http://localhost:${PORT}/auth/callback`
        })

        if (data.access_token) {
            saveToken(data.access_token)
            if (state.streamer) connectIRC(state.streamer)
            res.send('<pre>Authorization successful! You can close this tab.\nIRC will connect automatically when the control panel loads.</pre>')
        } else {
            res.send(`<pre>Error from Twitch: ${JSON.stringify(data, null, 2)}</pre>`)
        }
    } catch (err) {
        res.send(`<pre>Request failed: ${err.message}</pre>`)
    }
})

// ---------------------------------------------------------------------------
// WebSocket sync
// ---------------------------------------------------------------------------

// In-memory shared state
const state = {
    streamer: '',
    playedSongs: {} // { streamerName: [songId, ...] }
}

wss.on('connection', (ws) => {
    // Send current state to the newly connected client
    ws.send(JSON.stringify({ type: 'state_sync', payload: state }))

    ws.on('message', (raw) => {
        let msg
        try {
            msg = JSON.parse(raw.toString())
        } catch {
            return
        }

        switch (msg.type) {
            case 'client_state_push': {
                // Control panel pushes its localStorage state on connect (handles server restarts)
                const prevStreamer = state.streamer
                if (msg.payload.streamer) state.streamer = msg.payload.streamer
                if (msg.payload.playedSongs) {
                    for (const [k, v] of Object.entries(msg.payload.playedSongs)) {
                        if (!state.playedSongs[k]) state.playedSongs[k] = []
                        for (const id of v) {
                            if (!state.playedSongs[k].includes(id)) state.playedSongs[k].push(id)
                        }
                    }
                }
                // If the streamer changed, notify other connected clients (e.g. overlay) and connect IRC
                if (state.streamer && state.streamer !== prevStreamer) {
                    const notify = JSON.stringify({ type: 'set_streamer', payload: { streamer: state.streamer } })
                    wss.clients.forEach(client => {
                        if (client !== ws && client.readyState === WebSocket.OPEN) client.send(notify)
                    })
                    if (ircChannel !== `#${state.streamer.toLowerCase()}`) {
                        connectIRC(state.streamer)
                    }
                }
                break
            }

            case 'set_streamer':
                state.streamer = msg.payload.streamer
                if (msg.payload.streamer && ircChannel !== `#${msg.payload.streamer.toLowerCase()}`) {
                    connectIRC(msg.payload.streamer)
                }
                break

            case 'spin_command': {
                const { streamer, songId, queuePosition } = msg.payload
                if (streamer && songId) {
                    if (!state.playedSongs[streamer]) state.playedSongs[streamer] = []
                    if (!state.playedSongs[streamer].includes(songId)) {
                        state.playedSongs[streamer].push(songId)
                    }
                }
                if (queuePosition && serverConfig.autoPlay !== false) {
                    sendChat(`!setSong ${queuePosition} to 1`)
                }
                break
            }

            case 'close_winner_modal':
                if (serverConfig.autoPlay !== false) sendChat('!setPlayed')
                break

            case 'reset_played':
                if (msg.payload.streamer) state.playedSongs[msg.payload.streamer] = []
                break
        }

        // Broadcast to all connected clients (including sender so all UIs stay in sync)
        const data = JSON.stringify(msg)
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(data)
            }
        })
    })

    ws.on('error', (err) => console.error('[WS] Client error:', err.message))
})

// ---------------------------------------------------------------------------
// Start
// ---------------------------------------------------------------------------

const PORT = process.env.PORT || 3000
server.listen(PORT, () => {
    console.log(`Song Spinner server running`)
    console.log(`  Overlay (Browser Source): http://localhost:${PORT}/`)
    console.log(`  Control panel (Dock):     http://localhost:${PORT}/control`)

    const { username, clientId } = serverConfig.twitch
    const token = loadToken()
    if (token && username) {
        console.log(`  Twitch chat: enabled as ${username}`)
        // Validate token scopes on startup
        const req = https.request({
            hostname: 'id.twitch.tv',
            path: '/oauth2/validate',
            headers: { Authorization: `OAuth ${token}` }
        }, (res) => {
            let data = ''
            res.on('data', chunk => data += chunk)
            res.on('end', () => {
                try {
                    const info = JSON.parse(data)
                    if (info.status === 401) {
                        console.log(`  [Auth] Token is invalid or expired — delete server-token.json and visit http://localhost:${PORT}/auth`)
                    } else {
                        console.log(`  [Auth] Token valid. Login: ${info.login}, Scopes: ${(info.scopes || []).join(', ') || 'none'}`)
                        if (info.login && info.login.toLowerCase() !== username.toLowerCase()) {
                            console.log(`  [Auth] WARNING: Token belongs to "${info.login}" but username is set to "${username}" — update username in server-config.js to match`)
                        }
                        const scopes = info.scopes || []
                        if (!scopes.includes('chat:edit') || !scopes.includes('chat:read')) {
                            console.log(`  [Auth] Missing required scopes — delete server-token.json and visit http://localhost:${PORT}/auth`)
                        }
                    }
                } catch { /* ignore parse errors */ }
            })
        })
        req.on('error', () => {})
        req.end()
    } else if (clientId) {
        console.log(`  Twitch chat: visit http://localhost:${PORT}/auth to authorize`)
    } else {
        console.log(`  Twitch chat: disabled (fill in clientId/clientSecret in server-config.js to enable)`)
    }
})
