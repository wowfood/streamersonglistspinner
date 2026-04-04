;(function(ns) {
    // Initializes app config, theme, wheel, event handlers, and default streamer.
    ns.initialize = async function initialize() {
        await ns.loadConfig()
        ns.applyThemeConfig()
        ns.applyBackgroundConfig()
        ns.applyPlayedListPosition()

        ns.createWheel([{ label: "Enter streamer name above" }])
        ns.setupWheelResizeObserver()
        ns.setupResizeHandlers()

        const defaultStreamer = (ns.state.appConfig.streamer?.defaultName || "").trim()
        if(defaultStreamer) {
            ns.dom.streamerInput.value = defaultStreamer

            if(ns.state.appConfig.streamer?.hideChangeOptionWhenDefault) {
                ns.dom.streamerInput.style.display = "none"
                ns.dom.changeStreamerBtn.style.display = "none"
                ns.dom.streamerLabel.style.display = "none"
            }

            await ns.loadStreamer()
        }

        ns.updateStats()
        ns.updatePlayedList()
        ns.startStreamerAutoRefresh()
    }

    ns.startStreamerAutoRefresh = function startStreamerAutoRefresh() {
        if (ns.state.autoRefreshRunning) return
        ns.state.autoRefreshRunning = true

        const loop = async () => {
            while (ns.state.autoRefreshRunning) {
                const value = ns.state.streamer?.trim()

                if (value) {
                    try {
                        await ns.refreshQueueData()
                    } catch (err) {
                        console.error("Auto refresh failed:", err)
                    }
                }

                await new Promise(resolve => setTimeout(resolve, 30000))
            }
        }

        loop()
    }

    // Binds global handlers used by inline HTML event attributes.
    window.spin = () => ns.spin()
    window.loadStreamer = () => ns.loadStreamer()
    window.changeStreamer = () => ns.changeStreamer()
    window.toggleWheel = (checkbox) => ns.toggleWheel(checkbox)
    window.resetPlayed = () => ns.resetPlayed()
    window.closeWinnerModal = () => ns.closeWinnerModal()
    window.togglePlayedListCollapse = () => ns.togglePlayedListCollapse()

    // Allows closing the winner modal with Escape.
    document.addEventListener("keydown", (event) => {
        if(event.key === "Escape") {
            ns.closeWinnerModal()
        }
    })

    // Start the application, then connect to the sync server as the control panel.
    ns.initialize().then(() => ns.sync.init('control'))
}) (window.SongSpinner)
