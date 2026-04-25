window.SpinnerInterop = (function () {
    let _wheel = null
    let _isResizing = false
    let _savedWidth = null
    let _savedMinWidth = null
    let _resizeTimeout = null

    return {
        createWheel(items, colors) {
            const container = document.getElementById('wheelContainer')
            if (!container) return
            if (_wheel) {
                _wheel.remove();
                _wheel = null
            }
            _wheel = new spinWheel.Wheel(container, {
                items,
                itemBackgroundColors: colors,
                borderWidth: 0,
                lineWidth: 0,
                radius: 0.95,
                isInteractive: false
            })
        },

        spinToItem(index, duration) {
            if (_wheel) _wheel.spinToItem(index, duration)
        },

        getItems() {
            return _wheel ? _wheel.items : []
        },

        setupResizeObserver() {
            const container = document.getElementById('wheelContainer')
            if (!container || !window.ResizeObserver) return
            new ResizeObserver(() => {
                if (_wheel && !_isResizing) {
                    clearTimeout(_resizeTimeout)
                    _resizeTimeout = setTimeout(() => {
                        if (_wheel) {
                            const items = _wheel.items
                            _wheel.remove()
                            _wheel = new spinWheel.Wheel(container, {
                                items,
                                itemBackgroundColors: _wheel?.itemBackgroundColors || [],
                                borderWidth: 0,
                                lineWidth: 0,
                                radius: 0.95,
                                isInteractive: false
                            })
                        }
                    }, 200)
                }
            }).observe(container)
        },

        setupResizeHandlers(dotNetRef) {
            const handle = document.getElementById('resizeHandle')
            const playedList = document.getElementById('playedList')
            if (!handle || !playedList) return

            handle.addEventListener('mousedown', e => {
                e.preventDefault()
                _isResizing = true
                document.body.style.cursor = 'ew-resize'
                document.body.style.userSelect = 'none'
            })

            document.addEventListener('mousemove', e => {
                if (!_isResizing) return
                e.preventDefault()
                const containerRect = document.getElementById('container').getBoundingClientRect()
                const position = playedList.dataset.position || 'right'
                let newWidth = position === 'left'
                    ? e.clientX - containerRect.left - 10
                    : containerRect.right - e.clientX - 10
                const minPx = 300, maxPx = 800
                if (newWidth >= minPx && newWidth <= maxPx) {
                    const pct = (newWidth / containerRect.width) * 100
                    playedList.style.width = `${pct}%`
                    playedList.style.minWidth = `${minPx}px`
                }
            })

            document.addEventListener('mouseup', async () => {
                if (!_isResizing) return
                _isResizing = false
                document.body.style.cursor = 'default'
                document.body.style.userSelect = 'auto'
                const w = playedList.style.width
                const mw = playedList.style.minWidth
                if (w && dotNetRef) {
                    await dotNetRef.invokeMethodAsync('OnResizeEnd', w, mw)
                }
            })
        },

        applyTheme(colors, playedList) {
            const r = document.documentElement
            if (!colors) return
            r.style.setProperty('--app-text-color', colors.text || '')
            r.style.setProperty('--app-status-bg', colors.statusBackground || '')
            r.style.setProperty('--app-played-list-bg', colors.playedListBackground || '')
            r.style.setProperty('--app-played-item-bg', colors.playedItemBackground || '')
            r.style.setProperty('--app-resize-handle-bg', colors.resizeHandleBackground || '')
            r.style.setProperty('--app-resize-handle-hover-bg', colors.resizeHandleHoverBackground || '')
            r.style.setProperty('--app-toggle-bg', colors.toggleBackground || '')
            r.style.setProperty('--app-button-bg', colors.buttonBackground || '')
            r.style.setProperty('--app-button-text', colors.buttonText || '')
            r.style.setProperty('--app-pointer-color', colors.pointer || '')
            if (playedList) {
                r.style.setProperty('--app-played-list-font-family', playedList.fontFamily || '')
                r.style.setProperty('--app-played-list-font-size', playedList.fontSize || '')
                r.style.setProperty('--app-played-list-max-lines', playedList.maxLines ?? '')
            }
        },

        applyBackground(background) {
            if (!background) return
            const mode = (background.mode || 'color').toLowerCase()
            document.body.style.backgroundColor = background.color || ''
            if (mode === 'transparent' || mode === 'transparant') {
                document.body.style.backgroundColor = 'transparent'
                document.body.style.backgroundImage = 'none'
            } else if (mode === 'color') {
                document.body.style.backgroundImage = 'none'
            }
        },

        resetBackground() {
            document.body.style.backgroundColor = ''
            document.body.style.backgroundImage = ''
        },

        applyPlayedListPosition(position) {
            const container = document.getElementById('container')
            const icon = document.getElementById('collapseIcon')
            if (!container || !icon) return
            if ((position || 'right').toLowerCase() === 'left') {
                container.classList.add('played-list-left')
                icon.innerText = '▶'
            } else {
                container.classList.remove('played-list-left')
                icon.innerText = '◀'
            }
        },

        runConfetti(colors) {
            const el = document.getElementById('winnerConfetti')
            if (!el) return
            el.innerHTML = ''
            const palette = colors || ['#ff6b6b', '#4ecdc4', '#45b7d1', '#f9ca24']
            for (let i = 0; i < 36; i++) {
                const piece = document.createElement('span')
                piece.className = 'winner-confetti-piece'
                piece.style.left = `${Math.random() * 100}%`
                piece.style.backgroundColor = palette[i % palette.length]
                piece.style.animationDelay = `${Math.random() * 0.5}s`
                piece.style.animationDuration = `${1.2 + Math.random() * 1.1}s`
                el.appendChild(piece)
            }
        },

        setWheelVisible(visible) {
            const el = document.getElementById('wheelContents')
            if (el) el.style.display = visible ? 'flex' : 'none'
        },

        setPlayedListCollapsed(collapsed, position) {
            const el = document.getElementById('playedList')
            const icon = document.getElementById('collapseIcon')
            if (!el || !icon) return
            const pos = (position || 'right').toLowerCase()
            if (collapsed) {
                _savedWidth = el.style.width || ''
                _savedMinWidth = el.style.minWidth || ''
                el.classList.add('collapsed')
                el.style.width = '3rem'
                el.style.minWidth = '3rem'
                icon.innerText = pos === 'left' ? '◀' : '▶'
            } else {
                el.classList.remove('collapsed')
                icon.innerText = pos === 'left' ? '▶' : '◀'
                el.style.width = _savedWidth || ''
                el.style.minWidth = _savedMinWidth || ''
            }
        },

        setPlayedListWidth(width, minWidth) {
            const el = document.getElementById('playedList')
            if (el) {
                el.style.width = width
                el.style.minWidth = minWidth
            }
        }
    }
})()
