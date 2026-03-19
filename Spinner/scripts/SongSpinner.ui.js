;(function(ns) {
    // Creates or recreates the wheel instance for the given items.
    ns.createWheel = function createWheel(items) {
        if(ns.state.wheel) {
            ns.state.wheel.remove()
        }

        ns.state.wheel = new spinWheel.Wheel(ns.dom.wheelContainer, {
            items,
            itemBackgroundColors: ns.getWheelColors(),
            borderWidth: 0,
            lineWidth: 0,
            radius: 0.95,
            isInteractive: false
        })
    }

    // Updates played and available song counters.
    ns.updateStats = function updateStats() {
        ns.dom.playedCount.innerText = ns.state.playedSongs.length
        if(ns.dom.availableCount) {
            ns.dom.availableCount.innerText = Math.max(0, ns.state.allSongs.length - ns.state.playedSongs.length)
        }
    }

    // Renders the played songs list in reverse chronological order.
    ns.updatePlayedList = function updatePlayedList() {
        ns.dom.playedSongsUl.innerHTML = ""

        if(!ns.state.streamer || !ns.state.allSongs.length) {
            ns.updateStats()
            return
        }

        for(let i = ns.state.playedSongs.length - 1; i >= 0; i--) {
            const songId = ns.state.playedSongs[i]
            const song = ns.state.allSongs.find(s => s.id === songId)
            if(song) {
                const li = document.createElement("li")
                li.innerText = ns.createPlayedSongText(song)
                ns.dom.playedSongsUl.appendChild(li)
            }
        }

        ns.updateStats()
    }

    // Switches back to manual streamer input mode.
    ns.changeStreamer = function changeStreamer() {
        ns.dom.streamerInput.style.display = "block"
        ns.dom.streamerLabel.style.display = "none"
        ns.dom.changeStreamerBtn.style.display = "none"
        ns.dom.resetBtn.style.display = "none"
        ns.dom.streamerInput.value = ""
        ns.dom.streamerInput.focus()
    }

    // Shows or hides the wheel panel based on checkbox state.
    ns.toggleWheel = function toggleWheel(checkbox) {
        ns.dom.wheelContents.style.display = checkbox.checked ? "flex" : "none"
    }

    // Builds simple celebratory confetti animation pieces.
    ns.runWinnerFanfare = function runWinnerFanfare() {
        if(!ns.dom.winnerConfetti) return

        ns.dom.winnerConfetti.innerHTML = ""

        const palette = ns.getWheelColors()
        const count = 36

        for(let i = 0; i < count; i++) {
            const piece = document.createElement("span")
            piece.className = "winner-confetti-piece"
            piece.style.left = `${Math.random() * 100}%`
            piece.style.backgroundColor = palette[i % palette.length]
            piece.style.animationDelay = `${Math.random() * 0.5}s`
            piece.style.animationDuration = `${1.2 + Math.random() * 1.1}s`
            ns.dom.winnerConfetti.appendChild(piece)
        }
    }

    // Shows the winner modal with main line and detail fields.
    ns.showWinnerModal = function showWinnerModal(song) {
        if(!ns.dom.winnerModal) return

        const artist = song?.song?.artist || "Unknown Artist"
        const title = song?.song?.title || "Unknown Title"
        ns.dom.winnerMainLine.innerText = `${artist} - ${title}`
        ns.dom.winnerDetails.innerText = ns.createSongTextForFields(song, ns.getWinnerFields())

        ns.dom.winnerModal.style.display = "block"
        ns.runWinnerFanfare()
    }

    // Closes winner modal.
    ns.closeWinnerModal = function closeWinnerModal() {
        if(!ns.dom.winnerModal) return
        ns.dom.winnerModal.style.display = "none"
    }

    // Wires mouse handlers for resizing the played list panel.
    ns.setupResizeHandlers = function setupResizeHandlers() {
        if(!ns.dom.resizeHandle || !ns.dom.playedListEl) {
            return
        }

        ns.dom.resizeHandle.addEventListener("mousedown", (e) => {
            e.preventDefault()
            ns.state.isResizing = true
            document.body.style.cursor = "ew-resize"
            document.body.style.userSelect = "none"
        })

        document.addEventListener("mousemove", (e) => {
            if(!ns.state.isResizing) return
            e.preventDefault()

            const containerRect = document.getElementById("container").getBoundingClientRect()
            const newWidth = containerRect.right - e.clientX - 10
            const minWidth = 300 // 18.75rem
            const maxWidth = 800 // 50rem

            if(newWidth >= minWidth && newWidth <= maxWidth) {
                ns.dom.playedListEl.style.width = `${newWidth}px`
                ns.dom.playedListEl.style.minWidth = `${minWidth}px`
            }
        })

        document.addEventListener("mouseup", () => {
            if(ns.state.isResizing) {
                ns.state.isResizing = false
                document.body.style.cursor = "default"
                document.body.style.userSelect = "auto"
            }
        })
    }

    // Resizes wheel instance after container resize settles.
    ns.setupWheelResizeObserver = function setupWheelResizeObserver() {
        const resizeObserver = new ResizeObserver(() => {
            if(ns.state.wheel && !ns.state.isResizing) {
                clearTimeout(ns.state.resizeTimeout)
                ns.state.resizeTimeout = setTimeout(() => {
                    const currentItems = ns.state.wheel.items
                    ns.createWheel(currentItems)
                }, 200)
            }
        })

        resizeObserver.observe(ns.dom.wheelContainer)
    }
}) (window.SongSpinner)
