;(function(ns) {
    // Fetches play history for the current stream from the API and updates state.playedSongs.
    ns.fetchPlayedSongs = async function fetchPlayedSongs() {
        if(!ns.state.streamer) {
            ns.state.playedSongs = []
            return
        }

        const encodedStreamer = encodeURIComponent(ns.state.streamer).trim().toLowerCase()
        const period = ns.state.appConfig.songList?.playHistoryPeriod || ns.defaultConfig.songList.playHistoryPeriod
        const url = `https://api.streamersonglist.com/v1/streamers/${encodedStreamer}/playHistory?size=200&current=0&period=${encodeURIComponent(period)}`

        try {
            const res = await fetch(url)
            if(!res.ok) throw new Error(`HTTP ${res.status}`)
            const data = await res.json()
            ns.state.playedSongs = data.items || []
        } catch(e) {
            console.warn("Failed to fetch play history:", e.message)
            ns.state.playedSongs = []
        }
    }

    // Matches a queue item against a play history item by ID, falling back to artist+title.
    ns.songMatchesPlayed = function songMatchesPlayed(queueItem, playedItem) {
        const qSong = queueItem?.song
        const pSong = playedItem?.song
        if(!qSong || !pSong) return false
        if(qSong.id != null && pSong.id != null) return qSong.id === pSong.id
        return qSong.artist?.toLowerCase() === pSong.artist?.toLowerCase()
            && qSong.title?.toLowerCase() === pSong.title?.toLowerCase()
    }

    // Fetches queue and play history concurrently, updates wheel items, stats, and played list.
    // Returns the filtered available songs array.
    ns.refreshQueueData = async function refreshQueueData() {
        const [queueRes] = await Promise.all([
            fetch(ns.state.api),
            ns.fetchPlayedSongs()
        ])

        if(!queueRes.ok) throw new Error(`HTTP ${queueRes.status}: ${queueRes.statusText}`)

        const data = await queueRes.json()
        ns.state.allSongs = data.list || []

        const shouldExclude = ns.state.appConfig.songList?.excludePlayedSongs !== false
        const availableSongs = shouldExclude
            ? ns.state.allSongs.filter(song => !ns.state.playedSongs.some(played => ns.songMatchesPlayed(song, played)))
            : ns.state.allSongs

        const items = availableSongs.map(song => ({ label: ns.buildWheelLabel(song) }))
        ns.state.wheel.items = items.length ? items : [{ label: "No songs in queue" }]

        ns.updateStats()
        ns.updatePlayedList()

        return availableSongs
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
