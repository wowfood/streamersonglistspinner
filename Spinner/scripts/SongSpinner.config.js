;(function(ns) {
    // Recursively merges user config onto defaults.
    ns.mergeConfig = function mergeConfig(base, override) {
        const output = { ...base }

        for(const key of Object.keys(override || {})) {
            const overrideValue = override[key]
            const baseValue = base[key]

            if(Array.isArray(overrideValue)) {
                output[key] = [...overrideValue]
                continue
            }

            if(overrideValue && typeof overrideValue === "object" && baseValue && typeof baseValue === "object" && !Array.isArray(baseValue)) {
                output[key] = ns.mergeConfig(baseValue, overrideValue)
                continue
            }

            output[key] = overrideValue
        }

        return output
    }

    // Loads config from config.js global object (and keeps json loading disabled).
    ns.loadConfig = async function loadConfig() {
        const scriptConfig = window.SONG_SPINNER_CONFIG
        if(scriptConfig && typeof scriptConfig === "object") {
            ns.state.appConfig = ns.mergeConfig(ns.defaultConfig, scriptConfig)
            return
        }

        ns.state.appConfig = JSON.parse(JSON.stringify(ns.defaultConfig))
    }

    // Applies configured color values onto CSS custom properties.
    ns.applyThemeConfig = function applyThemeConfig() {
        const root = document.documentElement
        const colors = ns.state.appConfig.colors || {}
        const defaults = ns.defaultConfig.colors

        root.style.setProperty("--app-text-color", colors.text || defaults.text)
        root.style.setProperty("--app-status-bg", colors.statusBackground || defaults.statusBackground)
        root.style.setProperty("--app-played-list-bg", colors.playedListBackground || defaults.playedListBackground)
        root.style.setProperty("--app-played-item-bg", colors.playedItemBackground || defaults.playedItemBackground)
        root.style.setProperty("--app-resize-handle-bg", colors.resizeHandleBackground || defaults.resizeHandleBackground)
        root.style.setProperty("--app-resize-handle-hover-bg", colors.resizeHandleHoverBackground || defaults.resizeHandleHoverBackground)
        root.style.setProperty("--app-toggle-bg", colors.toggleBackground || defaults.toggleBackground)
        root.style.setProperty("--app-button-bg", colors.buttonBackground || defaults.buttonBackground)
        root.style.setProperty("--app-button-text", colors.buttonText || defaults.buttonText)
        root.style.setProperty("--app-pointer-color", colors.pointer || defaults.pointer)

        const playedList = ns.state.appConfig.playedList || {}
        const plDefaults = ns.defaultConfig.playedList
        root.style.setProperty("--app-played-list-font-family", playedList.fontFamily || plDefaults.fontFamily)
        root.style.setProperty("--app-played-list-font-size", playedList.fontSize || plDefaults.fontSize)
        root.style.setProperty("--app-played-list-max-lines", playedList.maxLines != null ? playedList.maxLines : plDefaults.maxLines)
    }

    // Applies configured body background mode (color, transparent, image).
    ns.applyBackgroundConfig = function applyBackgroundConfig() {
        const background = ns.state.appConfig.background || ns.defaultConfig.background
        const mode = (background.mode || "image").toLowerCase()
        const color = background.color || ns.defaultConfig.background.color

        document.body.style.backgroundColor = color

        if(mode === "transparent" || mode === "transparant") {
            document.body.style.backgroundColor = "transparent"
            document.body.style.backgroundImage = "none"
            return
        }

        if(mode === "color") {
            document.body.style.backgroundImage = "none"
            return
        }

        const image = background.image || ns.defaultConfig.background.image
        document.body.style.backgroundImage = `url('${image}')`
    }

    // Returns configured wheel colors or defaults.
    ns.getWheelColors = function getWheelColors() {
        const colors = ns.state.appConfig.wheelColors
        return Array.isArray(colors) && colors.length > 0 ? colors : ns.defaultConfig.wheelColors
    }
}) (window.SongSpinner)
