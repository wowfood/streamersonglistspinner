module.exports = {
    twitch: {
        // Your Twitch username (lowercase)
        username: '',

        // --- OAuth app credentials ---
        // 1. Go to https://dev.twitch.tv/console and click "Register Your Application"
        // 2. Set name to anything (e.g. "Song Spinner"), category to "Chat Bot"
        // 3. Set OAuth Redirect URL to: http://localhost:3000/auth/callback
        // 4. Copy the Client ID here, then click "New Secret" and copy that too
        clientId: '',
        clientSecret: ''

        // oauthToken is stored automatically in server-token.json after you visit http://localhost:3000/auth
    },

    // --- AutoPlay IRC commands ---
    // Automatically send StreamerSongList bot commands via Twitch chat when songs are selected.
    // Requires Twitch chat to be configured above.
    // Set to false to disable: !setSong on spin + !setPlayed on modal close.
    autoPlay: true
}
