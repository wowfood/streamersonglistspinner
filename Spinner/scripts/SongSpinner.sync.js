;(function(ns) {
    const sync = ns.sync = {}

    let ws = null
    let role = 'control' // 'control' or 'overlay'
    let reconnectTimer = null

    sync.init = function(clientRole) {
        role = clientRole
        connect()
    }

    sync.send = function(type, payload) {
        if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify({ type, payload }))
        }
    }

    function connect() {
        if (reconnectTimer) {
            clearTimeout(reconnectTimer)
            reconnectTimer = null
        }

        const wsUrl = `ws://${location.host}`
        ws = new WebSocket(wsUrl)

        ws.onopen = function() {
            console.log('[Sync] Connected as', role)

            if (role === 'control') {
                // Push current streamer to server so the overlay can sync on reconnect
                sync.send('client_state_push', {
                    streamer: ns.state.streamer || ''
                })
            }
        }

        ws.onmessage = function(event) {
            let msg
            try {
                msg = JSON.parse(event.data)
            } catch {
                return
            }
            handleMessage(msg)
        }

        ws.onclose = function() {
            console.log('[Sync] Disconnected — reconnecting in 3s')
            reconnectTimer = setTimeout(connect, 3000)
        }

        ws.onerror = function(err) {
            console.error('[Sync] WebSocket error:', err)
        }
    }

    function handleMessage(msg) {
        if (role === 'overlay') {
            handleOverlayMessage(msg)
        } else {
            handleControlMessage(msg)
        }
    }

    function handleOverlayMessage(msg) {
        switch (msg.type) {
            case 'state_sync': {
                const { streamer, playedSongs } = msg.payload
                // Restore played songs from server state
                if (playedSongs) {
                    const raw = {}
                    for (const [k, ids] of Object.entries(playedSongs)) {
                        raw[k] = ids
                    }
                    localStorage.setItem('streamerPlayedSongs', JSON.stringify(raw))
                }
                // Load streamer if one is active
                if (streamer) {
                    ns.dom.streamerInput.value = streamer
                    ns.loadStreamer(true) // silent — don't re-broadcast
                }
                break
            }
            case 'set_streamer':
                ns.dom.streamerInput.value = msg.payload.streamer
                ns.loadStreamer(true) // silent — don't re-broadcast
                break

            case 'spin_command':
                ns.spinToSong(msg.payload.songId, msg.payload.songData)
                break

            case 'reset_played':
                ns.resetPlayed(true)
                break

            case 'set_collapse': {
                const isCollapsed = ns.dom.playedListEl?.classList.contains('collapsed')
                if(msg.payload.collapsed !== isCollapsed) ns.togglePlayedListCollapse(true)
                break
            }

            case 'set_played_list_width':
                if(ns.dom.playedListEl) {
                    ns.dom.playedListEl.style.width = msg.payload.width
                    ns.dom.playedListEl.style.minWidth = msg.payload.minWidth
                }
                break

            case 'set_wheel_visible': {
                const cb = document.getElementById('showWheelCheckbox')
                if (cb) {
                    cb.checked = msg.payload.visible
                    ns.toggleWheel(cb, true)
                }
                break
            }

            case 'close_winner_modal':
                ns.closeWinnerModal(true)
                break
        }
    }

    function handleControlMessage(msg) {
        switch (msg.type) {
            case 'state_sync': {
                // Merge server's played songs into localStorage (handles reconnect after server restart)
                const { playedSongs } = msg.payload
                if (!playedSongs) break
                const raw = localStorage.getItem('streamerPlayedSongs')
                const local = raw ? JSON.parse(raw) : {}
                for (const [streamer, ids] of Object.entries(playedSongs)) {
                    if (!local[streamer]) local[streamer] = []
                    for (const id of ids) {
                        if (!local[streamer].includes(id)) local[streamer].push(id)
                    }
                }
                localStorage.setItem('streamerPlayedSongs', JSON.stringify(local))
                break
            }

            case 'close_winner_modal':
                ns.closeWinnerModal(true)
                break
        }
    }

})(window.SongSpinner)
