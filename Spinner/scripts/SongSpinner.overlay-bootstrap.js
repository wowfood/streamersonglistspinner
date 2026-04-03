;(function(ns) {
    // Overlay-specific initialisation — no interactive controls, driven by WebSocket sync.
    ns.initializeOverlay = async function initializeOverlay() {
        await ns.loadConfig()
        ns.applyThemeConfig()
        ns.applyBackgroundConfig()
        ns.applyPlayedListPosition()

        ns.createWheel([{ label: 'Waiting for streamer...' }])
        ns.setupWheelResizeObserver()
        ns.setupResizeHandlers()

        ns.updateStats()
        ns.updatePlayedList()

        // Connect to the sync server as the overlay role
        ns.sync.init('overlay')
    }

    // Bind only the controls the overlay needs (modal close, escape key)
    window.closeWinnerModal = () => ns.closeWinnerModal()
    window.togglePlayedListCollapse = () => ns.togglePlayedListCollapse()
    window.toggleWheel = (checkbox) => ns.toggleWheel(checkbox)

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') ns.closeWinnerModal()
    })

    ns.initializeOverlay()
})(window.SongSpinner)
