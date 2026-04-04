;(function(ns) {
    // Loads queue for the current streamer and refreshes wheel + played list UI.
    // silent=true skips broadcasting set_streamer (used when the overlay responds to a sync command).
    ns.loadStreamer = async function loadStreamer(silent) {
        ns.state.streamer = ns.dom.streamerInput.value.trim()
        if(!ns.state.streamer) {
            ns.setStatus("Please enter a streamer name")
            return
        }

        const encodedStreamer = encodeURIComponent(ns.state.streamer)

        // Always use full API URL (no backend proxy in this app)
        ns.state.api = `https://api.streamersonglist.com/v1/streamers/${encodedStreamer}/queue`
        ns.setStatus("Loading songs...")

        try {
            const availableSongs = await ns.refreshQueueData()

            ns.dom.streamerInput.style.display = "none"
            ns.dom.streamerLabel.innerText = `Streamer: ${ns.state.streamer}`

            const usingLockedDefault = Boolean(ns.state.appConfig.streamer?.defaultName) && ns.state.appConfig.streamer?.hideChangeOptionWhenDefault
            ns.dom.streamerLabel.style.display = usingLockedDefault ? "none" : "inline"
            ns.dom.changeStreamerBtn.style.display = usingLockedDefault ? "none" : "inline"
            ns.dom.resetBtn.style.display = "inline"

            ns.setStatus(`Loaded ${availableSongs.length} songs. Press SPIN!`)
            if(!silent) ns.sync?.send('set_streamer', { streamer: ns.state.streamer })
        } catch(E) {
            console.error("Full error details:", E)
            console.error("Error name:", E.name)
            console.error("Error message:", E.message)
            console.error("Error stack:", E.stack)
            ns.setStatus(`API: ${ns.state.api}`)
            setTimeout(() => ns.setStatus(`Error: ${E.name} - ${E.message}`), 2000)
        }
    }

    // Spins the wheel, selects a winner, and starts cooldown countdown.
    ns.spin = async function spin() {
        if(!ns.state.api) {
            ns.setStatus("Please enter a streamer name first")
            return
        }

        const now = Date.now()
        if(now - ns.state.lastSpinTime < 1000) {
            ns.setStatus("Cooldown active (10s)")
            return
        }

        ns.state.lastSpinTime = now
        ns.dom.spinButton.disabled = true
        ns.setStatus("Fetching queue...")

        try {
            const availableSongs = await ns.refreshQueueData()

            if(availableSongs.length === 0) {
                ns.setStatus("No songs left to spin!")
                ns.dom.spinButton.disabled = false
                return
            }

            const winnerIndex = Math.floor(Math.random() * availableSongs.length)
            ns.setStatus("Spinning...")
            ns.state.wheel.spinToItem(winnerIndex, 5000)

            const winner = availableSongs[winnerIndex]
            const queuePosition = ns.state.allSongs.indexOf(winner) + 1
            ns.sync?.send('spin_command', {
                streamer: ns.state.streamer,
                songId: winner.song?.id,
                songData: winner,
                queuePosition
            })

            setTimeout(() => {
                ns.setStatus(`Winner: ${ns.state.wheel.items[winnerIndex]?.label || ns.buildWheelLabel(winner)}`)
                ns.showWinnerModal(winner)

                let countdown = 1
                ns.dom.spinButton.innerText = countdown

                if(ns.state.countdownInterval) {
                    clearInterval(ns.state.countdownInterval)
                }

                ns.state.countdownInterval = setInterval(() => {
                    countdown--
                    if(countdown > 0) {
                        ns.dom.spinButton.innerText = countdown
                    } else {
                        clearInterval(ns.state.countdownInterval)
                        ns.dom.spinButton.innerText = "SPIN"
                        ns.dom.spinButton.disabled = false
                    }
                }, 1000)
            }, 5000)
        } catch(E) {
            console.error("Full error details:", E)
            console.error("Error name:", E.name)
            console.error("Error message:", E.message)
            console.error("Error stack:", E.stack)
            ns.setStatus(`API: ${ns.state.api}`)
            setTimeout(() => ns.setStatus(`Error: ${E.name} - ${E.message}`), 2000)
            ns.dom.spinButton.disabled = false
        }
    }

    // Clears local played state for the current session (does not affect the API).
    // silent=true skips broadcasting (used when the overlay receives a reset_played command).
    ns.resetPlayed = function resetPlayed(silent) {
        if(!ns.state.streamer) return

        ns.state.playedSongs = []
        ns.setStatus(`Played songs reset for ${ns.state.streamer}`)
        ns.updatePlayedList()
        if(!silent) ns.sync?.send('reset_played', { streamer: ns.state.streamer })
    }

    // Spins the wheel to a specific song (used by the overlay when it receives a spin_command).
    ns.spinToSong = function spinToSong(songId, songData) {
        if(!ns.state.allSongs || !ns.state.allSongs.length) return

        const shouldExclude = ns.state.appConfig.songList?.excludePlayedSongs !== false
        const availableSongs = shouldExclude
            ? ns.state.allSongs.filter(song => !ns.state.playedSongs.some(played => ns.songMatchesPlayed(song, played)))
            : ns.state.allSongs

        const winnerIndex = availableSongs.findIndex(song => song.song?.id === songId)
        if(winnerIndex === -1) return

        const items = availableSongs.map(song => ({ label: ns.buildWheelLabel(song) }))
        ns.state.wheel.items = items
        ns.state.wheel.spinToItem(winnerIndex, 5000)

        const winner = availableSongs[winnerIndex]
        setTimeout(async () => {
            await ns.refreshQueueData()
            ns.showWinnerModal(winner)
        }, 5000)
    }
}) (window.SongSpinner)
