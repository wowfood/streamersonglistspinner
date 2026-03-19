;(function(ns) {
    // Loads queue for the current streamer and refreshes wheel + played list UI.
    ns.loadStreamer = async function loadStreamer() {
        ns.state.streamer = ns.dom.streamerInput.value.trim()
        if(!ns.state.streamer) {
            ns.setStatus("Please enter a streamer name")
            return
        }

        ns.loadPlayedSongsForStreamer()

        const encodedStreamer = encodeURIComponent(ns.state.streamer)

        // Always use full API URL (no backend proxy in this app)
        ns.state.api = `https://api.streamersonglist.com/v1/streamers/${encodedStreamer}/queue`
        ns.setStatus("Loading songs...")

        try {
            console.log("Fetching from:", ns.state.api)
            const res = await fetch(ns.state.api)
            console.log("Response status:", res.status, res.statusText)

            if(!res.ok) {
                throw new Error(`HTTP ${res.status}: ${res.statusText}`)
            }

            const data = await res.json()

            ns.state.allSongs = data.list || []

            const availableSongs = ns.state.allSongs.filter(song => !ns.state.playedSongs.includes(song.id))
            const items = availableSongs.map(song => ({ label: ns.buildWheelLabel(song) }))

            ns.state.wheel.items = items.length ? items : [{ label: "No songs in queue" }]

            ns.dom.streamerInput.style.display = "none"
            ns.dom.streamerLabel.innerText = `Streamer: ${ns.state.streamer}`

            const usingLockedDefault = Boolean(ns.state.appConfig.streamer?.defaultName) && ns.state.appConfig.streamer?.hideChangeOptionWhenDefault
            ns.dom.streamerLabel.style.display = usingLockedDefault ? "none" : "inline"
            ns.dom.changeStreamerBtn.style.display = usingLockedDefault ? "none" : "inline"
            ns.dom.resetBtn.style.display = "inline"

            ns.setStatus(`Loaded ${items.length} songs. Press SPIN!`)
            ns.updateStats()
            ns.updatePlayedList()
        } catch(E) {
            console.error("Full error details:", E)
            console.error("Error name:", E.name)
            console.error("Error message:", E.message)
            console.error("Error stack:", E.stack)
            ns.setStatus(`API: ${ns.state.api}`)
            setTimeout(() => ns.setStatus(`Error: ${E.name} - ${E.message}`), 2000)
        }
    }

    // Spins the wheel, selects a winner, persists it, and starts cooldown countdown.
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
            console.log("Fetching from:", ns.state.api)
            const res = await fetch(ns.state.api)
            console.log("Response status:", res.status, res.statusText)

            if(!res.ok) {
                throw new Error(`HTTP ${res.status}: ${res.statusText}`)
            }

            const data = await res.json()

            ns.state.allSongs = data.list || []

            const availableSongs = ns.state.allSongs.filter(song => !ns.state.playedSongs.includes(song.id))
            if(availableSongs.length === 0) {
                ns.setStatus("No songs left to spin!")
                ns.dom.spinButton.disabled = false
                return
            }

            const items = availableSongs.map(song => ({ label: ns.buildWheelLabel(song) }))
            ns.state.wheel.items = items

            const winnerIndex = Math.floor(Math.random() * items.length)
            ns.setStatus("Spinning...")
            ns.state.wheel.spinToItem(winnerIndex, 5000)

            setTimeout(() => {
                const winner = availableSongs[winnerIndex]
                ns.state.playedSongs.push(winner.id)
                ns.savePlayedSongsForStreamer()

                ns.setStatus(`Winner: ${items[winnerIndex].label}`)
                ns.showWinnerModal(winner)
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

    // Clears played history for the current streamer.
    ns.resetPlayed = function resetPlayed() {
        if(!ns.state.streamer) return

        ns.state.playedSongs = []
        ns.savePlayedSongsForStreamer()
        ns.setStatus(`Played songs reset for ${ns.state.streamer}`)
        ns.updatePlayedList()
    }
}) (window.SongSpinner)
