;(function(ns) {
    // Loads queue for the current streamer and refreshes wheel + played list UI.
    ns.loadStreamer = async function loadStreamer() {
        ns.state.streamer = ns.dom.streamerInput.value.trim()
        if(!ns.state.streamer) {
            ns.setStatus("Please enter a streamer name")
            return
        }

        const encodedStreamer = encodeURIComponent(ns.state.streamer).trim().toLowerCase()

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

            setTimeout(async () => {
                const winner = availableSongs[winnerIndex]
                ns.setStatus(`Winner: ${ns.state.wheel.items[winnerIndex]?.label || ns.buildWheelLabel(winner)}`)
                ns.showWinnerModal(winner)
                await ns.fetchPlayedSongs()
                ns.updatePlayedList()

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
    ns.resetPlayed = function resetPlayed() {
        if(!ns.state.streamer) return

        ns.state.playedSongs = []
        ns.setStatus(`Played songs reset for ${ns.state.streamer}`)
        ns.updatePlayedList()
    }
}) (window.SongSpinner)
