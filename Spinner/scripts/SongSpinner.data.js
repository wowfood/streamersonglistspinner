;(function(ns) {
    // Reads all streamer song-history data from localStorage.
    ns.getStreamerData = function getStreamerData() {
        return JSON.parse(localStorage.getItem("streamerPlayedSongs") || "{}")
    }

    // Persists streamer song-history data to localStorage.
    ns.saveStreamerData = function saveStreamerData(data) {
        localStorage.setItem("streamerPlayedSongs", JSON.stringify(data))
    }

    // Loads played song IDs for the currently selected streamer.
    ns.loadPlayedSongsForStreamer = function loadPlayedSongsForStreamer() {
        if(!ns.state.streamer) {
            ns.state.playedSongs = []
            return
        }

        const allData = ns.getStreamerData()
        ns.state.playedSongs = allData[ns.state.streamer] || []
    }

    // Saves played song IDs for the currently selected streamer.
    ns.savePlayedSongsForStreamer = function savePlayedSongsForStreamer() {
        if(!ns.state.streamer) return

        const allData = ns.getStreamerData()
        allData[ns.state.streamer] = ns.state.playedSongs
        ns.saveStreamerData(allData)
    }

    // Safely returns the primary requester name from a queue item.
    ns.getPrimaryRequester = function getPrimaryRequester(song) {
        return song?.requests?.[0]?.name || "Unknown"
    }

    // Builds a wheel label from the queue item fields.
    ns.buildWheelLabel = function buildWheelLabel(song) {
        return `${song.song.artist} - ${song.song.title} (${ns.getPrimaryRequester(song)})`
    }

    // Returns donation value from known API fields.
    ns.formatDonation = function formatDonation(song) {
        const request = song?.requests?.[0] || {}
        const donation = request.donationAmount ?? request.donation ?? request.amount ?? request.price
        if(donation === undefined || donation === null || donation === "") {
            return "None"
        }

        return `${donation}`
    }

    // Returns one display value for the requested list field.
    ns.getSongFieldValue = function getSongFieldValue(song, field) {
        switch((field || "").toLowerCase()) {
            case "artist":
                return song?.song?.artist || "Unknown"
            case "title":
                return song?.song?.title || "Unknown"
            case "requester":
                return ns.getPrimaryRequester(song)
            case "donation":
                return ns.formatDonation(song)
            default:
                return ""
        }
    }

    // Formats any list of fields into a single row text.
    ns.createSongTextForFields = function createSongTextForFields(song, fields) {
        const lines = fields
            .map(field => {
                const value = ns.getSongFieldValue(song, field)
                if(!value) {
                    return null
                }

                const label = field.charAt(0).toUpperCase() + field.slice(1)
                return `${label}: ${value}`
            })
            .filter(Boolean)

        return lines.join(" | ")
    }

    // Formats the played-list row using configured songList.fields.
    ns.createPlayedSongText = function createPlayedSongText(song) {
        const fields = Array.isArray(ns.state.appConfig.songList?.fields) && ns.state.appConfig.songList.fields.length > 0
            ? ns.state.appConfig.songList.fields
            : ns.defaultConfig.songList.fields

        return ns.createSongTextForFields(song, fields)
    }

    // Returns winner modal fields while always enforcing requester.
    ns.getWinnerFields = function getWinnerFields() {
        const configuredFields = Array.isArray(ns.state.appConfig.songList?.fields) && ns.state.appConfig.songList.fields.length > 0
            ? ns.state.appConfig.songList.fields
            : ns.defaultConfig.songList.fields

        const normalized = configuredFields.map(f => `${f}`.toLowerCase())
        if(!normalized.includes("requester")) {
            normalized.push("requester")
        }

        return normalized
    }
}) (window.SongSpinner)
