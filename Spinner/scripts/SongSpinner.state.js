// Shared SongSpinner namespace and mutable app state.
window.SongSpinner = window.SongSpinner || {}

;(function(ns) {
    // Default configuration used when no external config is supplied.
    ns.defaultConfig = {
        debug: true,
        wheelColors: ["#ff6b6b", "#4ecdc4", "#45b7d1", "#f9ca24", "#6c5ce7", "#a29bfe", "#fd79a8", "#fdcb6e"],
        background: {
            mode: "color",
            color: "#111111",
            image: "background.jpg"
        },
        streamer: {
            defaultName: "",
            hideChangeOptionWhenDefault: true
        },
        songList: {
            fields: ["artist", "title", "requester"]
        },
        colors: {
            text: "#ffffff",
            statusBackground: "rgba(0, 0, 0, 0.7)",
            playedListBackground: "rgba(0, 0, 0, 0.7)",
            playedItemBackground: "#222",
            resizeHandleBackground: "#333",
            resizeHandleHoverBackground: "#555",
            toggleBackground: "#222",
            buttonBackground: "#ffffff",
            buttonText: "#111111",
            pointer: "wheat"
        }
    }

    // Runtime state that changes during user interaction.
    ns.state = {
        streamer: "",
        api: "",
        wheel: null,
        lastSpinTime: 0,
        allSongs: [],
        playedSongs: [],
        appConfig: JSON.parse(JSON.stringify(ns.defaultConfig)),
        countdownInterval: null,
        resizeTimeout: null,
        isResizing: false
    }

    // Cached DOM elements used throughout the app.
    ns.dom = {
        status: document.getElementById("status"),
        playedSongsUl: document.getElementById("playedSongsUl"),
        playedCount: document.getElementById("playedCount"),
        availableCount: document.getElementById("availableCount"),
        streamerInput: document.getElementById("streamerInput"),
        streamerLabel: document.getElementById("streamerLabel"),
        changeStreamerBtn: document.getElementById("changeStreamerBtn"),
        spinButton: document.getElementById("spinButton"),
        resetBtn: document.getElementById("resetBtn"),
        wheelContents: document.getElementById("wheelContents"),
        wheelContainer: document.getElementById("wheelContainer"),
        winnerModal: document.getElementById("winnerModal"),
        winnerMainLine: document.getElementById("winnerMainLine"),
        winnerDetails: document.getElementById("winnerDetails"),
        winnerConfetti: document.getElementById("winnerConfetti"),
        resizeHandle: document.getElementById("resizeHandle"),
        playedListEl: document.getElementById("playedList")
    }

    // Central status renderer honoring the debug config flag.
    ns.setStatus = function setStatus(message, isVisible) {
        const showStatus = isVisible !== false && ns.state.appConfig.debug !== false
        ns.dom.status.innerText = message || ""
        ns.dom.status.style.display = showStatus ? "block" : "none"
    }
})(window.SongSpinner)
