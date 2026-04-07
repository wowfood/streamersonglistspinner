const express = require('express')
const http = require('http')
const https = require('https')
const WebSocket = require('ws')
const tls = require('tls')
const path = require('path')
const fs = require('fs')

let serverConfig = { twitch: { username: '', clientId: '', clientSecret: '' }, autoPlay: true }
try {
    // server-config.json (written by /setup page) takes priority over server-config.js
    serverConfig = JSON.parse(fs.readFileSync(path.join(__dirname, 'server-config.json'), 'utf8'))
} catch {
    try {
        serverConfig = require('./server-config')
    } catch {
        // neither config found — chat commands disabled
    }
}

const TOKEN_FILE = path.join(__dirname, 'server-token.json')

function loadToken() {
    try {
        const data = JSON.parse(fs.readFileSync(TOKEN_FILE, 'utf8'))
        return { oauthToken: data.oauthToken || '', refreshToken: data.refreshToken || '' }
    } catch {
        return { oauthToken: '', refreshToken: '' }
    }
}

function saveToken(oauthToken, refreshToken) {
    fs.writeFileSync(TOKEN_FILE, JSON.stringify({ oauthToken, refreshToken }, null, 2))
    console.log('[Auth] Token saved to server-token.json')
}

async function refreshAccessToken() {
    const { refreshToken } = loadToken()
    const { clientId, clientSecret } = serverConfig.twitch
    if (!refreshToken || !clientId || !clientSecret) return false
    try {
        const data = await httpsPost('https://id.twitch.tv/oauth2/token', {
            client_id: clientId,
            client_secret: clientSecret,
            refresh_token: refreshToken,
            grant_type: 'refresh_token'
        })
        if (data.access_token) {
            saveToken(data.access_token, data.refresh_token || refreshToken)
            console.log('[Auth] Token refreshed successfully')
            return true
        }
        return false
    } catch {
        return false
    }
}

// ---------------------------------------------------------------------------
// Twitch IRC chat integration
// ---------------------------------------------------------------------------

let ircSocket = null
let ircChannel = null
let ircReady = false

function connectIRC(channel) {
    const { username } = serverConfig.twitch
    const { oauthToken } = loadToken()
    if (!username || !oauthToken) return

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
        ircSocket.write(`PASS oauth:${oauthToken}\r\n`)
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
            if (line.includes(' NOTICE ')) {
                console.log('[IRC]', line.trim())
                if (line.includes('Login authentication failed') || line.includes('Improperly formatted auth')) {
                    console.log('[Auth] IRC auth failed — attempting token refresh...')
                    ircChannel = null // prevent reconnect loop while refreshing
                    refreshAccessToken().then(success => {
                        if (success && channel) {
                            connectIRC(channel)
                        } else {
                            console.log(`[Auth] Refresh failed — visit http://localhost:${PORT}/auth to re-authorize`)
                        }
                    })
                }
            } else if (line.includes(' 001 ')) console.log('[IRC] Connected to Twitch IRC', line.trim())
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
app.use(express.urlencoded({ extended: false }))
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
        return res.redirect('/setup')
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
            saveToken(data.access_token, data.refresh_token || '')
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
// Setup page — configure Twitch credentials without editing files
// ---------------------------------------------------------------------------

function setupHtml(saved) {
    const cfg = serverConfig.twitch || {}
    const esc = (s) => (s || '').replace(/&/g, '&amp;').replace(/"/g, '&quot;')
    return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Song Spinner — Setup</title>
  <style>
    *, *::before, *::after { box-sizing: border-box; }
    body { font-family: sans-serif; max-width: 540px; margin: 40px auto; padding: 0 20px 60px; background: #1a1a2e; color: #ddd; }
    h1 { color: #fff; margin-bottom: 4px; }
    .subtitle { color: #888; margin: 0 0 28px; font-size: 0.9rem; }
    h2 { font-size: 1rem; color: #aaa; margin: 0 0 12px; }
    label { display: block; margin-bottom: 16px; font-size: 0.9rem; color: #ccc; }
    label span { display: block; margin-bottom: 5px; }
    label a { color: #888; font-size: 0.8rem; margin-left: 6px; }
    input[type=text], input[type=password] { width: 100%; padding: 8px 10px; background: #2a2a3e; border: 1px solid #444; border-radius: 4px; color: #fff; font-size: 0.95rem; }
    input[type=text]:focus, input[type=password]:focus { outline: none; border-color: #6c5ce7; }
    .check { display: flex; align-items: center; gap: 8px; cursor: pointer; }
    .check input { width: auto; }
    .btn { display: inline-block; padding: 10px 22px; border: none; border-radius: 4px; cursor: pointer; font-size: 0.9rem; text-decoration: none; }
    .btn-primary { background: #6c5ce7; color: #fff; }
    .btn-primary:hover { background: #7d6ff5; }
    .btn-twitch { background: #9147ff; color: #fff; margin-top: 4px; }
    .btn-twitch:hover { background: #a45cff; }
    .btn-back { color: #6c5ce7; font-size: 0.9rem; }
    .success { background: #1a3a2a; border: 1px solid #2d6a4f; color: #52b788; padding: 12px 16px; border-radius: 4px; margin-bottom: 24px; }
    .section { margin-top: 28px; padding-top: 24px; border-top: 1px solid #2a2a3e; }
    .note { font-size: 0.8rem; color: #777; margin: 8px 0 0; }
    .optional { font-size: 0.75rem; color: #666; margin-left: 6px; }
  </style>
</head>
<body>
  <h1>Song Spinner Setup</h1>
  <p class="subtitle">Configure Twitch chat integration for AutoPlay commands.<br>This is optional — the spinner works without it.</p>

  ${saved ? '<div class="success">&#10003; Settings saved.</div>' : ''}

  <form method="POST" action="/setup">
    <label>
      <span>Twitch Username</span>
      <input type="text" name="username" value="${esc(cfg.username)}" placeholder="your_twitch_name" autocomplete="off" spellcheck="false">
    </label>
    <label>
      <span>Client ID <a href="https://dev.twitch.tv/console" target="_blank">(get from dev.twitch.tv ↗)</a></span>
      <input type="text" name="clientId" value="${esc(cfg.clientId)}" placeholder="paste your Client ID here" autocomplete="off" spellcheck="false">
    </label>
    <label>
      <span>Client Secret</span>
      <input type="password" name="clientSecret" value="${esc(cfg.clientSecret)}" placeholder="paste your Client Secret here" autocomplete="off">
    </label>
    <label class="check">
      <input type="checkbox" name="autoPlay" ${serverConfig.autoPlay !== false ? 'checked' : ''}>
      <span>AutoPlay <span class="optional">(send !setSong and !setPlayed automatically)</span></span>
    </label>
    <br>
    <button type="submit" class="btn btn-primary">Save Settings</button>
  </form>

  <div class="section">
    <h2>Twitch Authorization</h2>
    ${cfg.clientId
        ? `<a class="btn btn-twitch" href="/auth">Authorize with Twitch &rarr;</a>
           <p class="note">Do this after saving your credentials above. You only need to do it once.</p>`
        : `<p class="note">Save your Client ID and Client Secret first — the Authorize button will appear here.</p>`
    }
  </div>

  <div class="section">
    <a class="btn-back" href="/control">&larr; Back to Control Panel</a>
  </div>
</body>
</html>`
}

app.get('/setup', (req, res) => {
    res.send(setupHtml(req.query.saved === '1'))
})

app.post('/setup', (req, res) => {
    const { username, clientId, clientSecret, autoPlay } = req.body
    serverConfig = {
        twitch: {
            username: (username || '').trim().toLowerCase(),
            clientId: (clientId || '').trim(),
            clientSecret: (clientSecret || '').trim()
        },
        autoPlay: autoPlay === 'on'
    }
    fs.writeFileSync(path.join(__dirname, 'server-config.json'), JSON.stringify(serverConfig, null, 2))
    if (serverConfig.twitch.username && serverConfig.twitch.clientId && state.streamer) {
        if (ircChannel !== `#${serverConfig.twitch.username}`) connectIRC(serverConfig.twitch.username)
    }
    res.redirect('/setup?saved=1')
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
    console.log(`  Setup (Twitch/AutoPlay):  http://localhost:${PORT}/setup`)

    const { username, clientId } = serverConfig.twitch
    const { oauthToken } = loadToken()
    if (oauthToken && username) {
        console.log(`  Twitch chat: enabled as ${username}`)
        // Validate token scopes on startup
        const req = https.request({
            hostname: 'id.twitch.tv',
            path: '/oauth2/validate',
            headers: { Authorization: `OAuth ${oauthToken}` }
        }, (res) => {
            let data = ''
            res.on('data', chunk => data += chunk)
            res.on('end', () => {
                try {
                    const info = JSON.parse(data)
                    if (info.status === 401) {
                        console.log('[Auth] Token expired — attempting refresh...')
                        refreshAccessToken().then(success => {
                            if (success) {
                                console.log('[Auth] Token refreshed — IRC will connect when the control panel loads')
                            } else {
                                console.log(`[Auth] Refresh failed — visit http://localhost:${PORT}/auth to re-authorize`)
                            }
                        })
                    } else {
                        console.log(`  [Auth] Token valid. Login: ${info.login}, Scopes: ${(info.scopes || []).join(', ') || 'none'}`)
                        if (info.login && info.login.toLowerCase() !== username.toLowerCase()) {
                            console.log(`  [Auth] WARNING: Token belongs to "${info.login}" but username is set to "${username}" — update username in the Setup page`)
                        }
                        const scopes = info.scopes || []
                        if (!scopes.includes('chat:edit') || !scopes.includes('chat:read')) {
                            console.log(`  [Auth] Missing required scopes — visit http://localhost:${PORT}/auth to re-authorize`)
                        }
                    }
                } catch { /* ignore parse errors */ }
            })
        })
        req.on('error', () => {})
        req.end()
    } else if (clientId) {
        console.log(`  Twitch chat: visit http://localhost:${PORT}/setup to configure`)
    } else {
        console.log(`  Twitch chat: disabled — visit http://localhost:${PORT}/setup to configure`)
    }
})
