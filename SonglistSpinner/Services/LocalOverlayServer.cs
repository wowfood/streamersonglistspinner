using System.Diagnostics;
using System.Net;
using System.Text;

namespace SonglistSpinner.Services;

public class LocalOverlayServer : IAsyncDisposable
{
    private const string OverlayHtml = """
                                       <!DOCTYPE html>
                                       <html lang="en">
                                       <head>
                                       <meta charset="UTF-8">
                                       <meta name="viewport" content="width=device-width, initial-scale=1.0">
                                       <title>Overlay — SonglistSpinner</title>
                                       <style>
                                       :root{
                                           --app-text-color: #ffffff;
                                           --app-status-bg: rgba(0, 0, 0, 0.7);
                                           --app-played-list-bg: rgba(0, 0, 0, 0.7);
                                           --app-played-item-bg: #222;
                                           --app-resize-handle-bg: #333;
                                           --app-resize-handle-hover-bg: #555;
                                           --app-toggle-bg: #222;
                                           --app-button-bg: #ffffff;
                                           --app-button-text: #111111;
                                           --app-pointer-color: wheat;
                                           --app-played-list-font-family: sans-serif;
                                           --app-played-list-font-size: 0.875rem;
                                           --app-played-list-max-lines: 1;
                                       }
                                       html, body {
                                           height: 100%;
                                       }
                                       body{
                                           background:#111;
                                           background-image: none;
                                           background-size: cover;
                                           background-position: center;
                                           background-repeat: no-repeat;
                                           background-attachment: fixed;
                                           color:var(--app-text-color);
                                           font-family:sans-serif;
                                           text-align:center;
                                           margin:0;
                                           padding:0;
                                           height: 100%;
                                           overflow:hidden;
                                       }
                                       #container{
                                           display:flex;
                                           justify-content: space-between;
                                           height: 100%;
                                           min-height: 0;
                                       }
                                       #container.played-list-left{
                                           flex-direction: row-reverse;
                                       }
                                       #wheelSection{
                                           flex:1;
                                           display:flex;
                                           flex-direction:column;
                                           align-items:center;
                                           justify-content:flex-start;
                                           padding-top:1.25rem;
                                           min-width:0;
                                           min-height:0;
                                       }
                                       #wheelContents{
                                           flex:1;
                                           display:flex;
                                           flex-direction:column;
                                           align-items:center;
                                           justify-content:flex-start;
                                           padding-top:1.25rem;
                                           min-width:0;
                                           min-height:0;
                                       }
                                       #buttonContainer{
                                           display:flex;
                                           gap:0.625rem;
                                       }
                                       #resizeHandle{
                                           width:0.625rem;
                                           background:var(--app-resize-handle-bg);
                                           cursor:ew-resize;
                                           position:relative;
                                       }
                                       #resizeHandle:hover{
                                           background:var(--app-resize-handle-hover-bg);
                                       }
                                       #playedListTop{
                                           display:flex;
                                           align-items:center;
                                           gap:0.625rem;
                                           flex-shrink: 0;
                                           padding:1.25rem;
                                           padding-bottom:0;
                                       }
                                       #container.played-list-left #playedListTop{
                                           flex-direction:row-reverse;
                                       }
                                       #playedList.collapsed #playedListTop{
                                           padding:0.5rem;
                                       }
                                       #streamerSection{
                                           display:flex;
                                           align-items:center;
                                           gap:0.625rem;
                                           flex-wrap:wrap;
                                           flex:1;
                                       }
                                       #streamerLabel{
                                           font-size:1rem;
                                           font-weight:bold;
                                       }
                                       .small-button{
                                           padding:0.3125rem 0.9375rem;
                                           font-size:0.875rem;
                                           margin:0;
                                       }
                                       #wheelContainer{
                                           width:clamp(6.25rem, 70vmin, 100%);
                                           height:clamp(6.25rem, 70vmin, 100%);
                                           aspect-ratio: 1;
                                           max-width: min(90vw, 100%);
                                           max-height: 80vh;
                                           flex-shrink:1;
                                       }
                                       #pointer{
                                           width:0;
                                           height:0;
                                           border-left:1.5625rem solid transparent;
                                           border-right:1.5625rem solid transparent;
                                           border-top:2.5rem solid var(--app-pointer-color);
                                       }
                                       button{
                                           margin-top:1.25rem;
                                           padding:0.75rem 1.875rem;
                                           font-size:1.25rem;
                                           cursor:pointer;
                                           background:var(--app-button-bg);
                                           color:var(--app-button-text);
                                       }
                                       #soungCounterSection{
                                           display:flex;
                                           align-items:center;
                                           justify-content:space-between;
                                           flex-direction: row;
                                           gap:0.625rem;
                                           margin-bottom:0.625rem;
                                       }
                                       #playedList{
                                           display: flex;
                                           flex-direction: column;
                                           height: 100%;
                                           min-height: 0;
                                           text-align:left;
                                           width:min(31.25rem, 40vw);
                                           min-width:18.75rem;
                                           max-width:50rem;
                                           overflow:hidden;
                                           background:var(--app-played-list-bg);
                                           transition: width 0.3s ease;
                                           position: relative;
                                       }
                                       #playedList.collapsed{
                                           display: none !important;
                                       }
                                       .collapse-button{
                                           background:var(--app-button-bg);
                                           color:var(--app-button-text);
                                           border:none;
                                           width:2rem;
                                           height:2rem;
                                           cursor:pointer;
                                           border-radius:0.25rem;
                                           font-size:1rem;
                                           display:flex;
                                           align-items:center;
                                           justify-content:center;
                                           padding:0;
                                           margin:0;
                                           flex-shrink:0;
                                       }
                                       .collapse-button:hover{
                                           opacity:0.8;
                                       }
                                       #playedListContent{
                                           display:flex;
                                           flex-direction:column;
                                           flex:1;
                                           min-height: 0;
                                           overflow:hidden;
                                           padding:0 1.25rem;
                                           padding-bottom:0;
                                       }
                                       #playedList h3{
                                           margin-top:0;
                                       }
                                       #playedList ul{
                                           list-style:none;
                                           padding:0;
                                           flex:1;
                                           overflow-y:auto;
                                           min-height: 0;
                                           margin:0;
                                       }
                                       #playedList li{
                                           padding:0.5rem;
                                           margin:0.25rem 0;
                                           background:var(--app-played-item-bg);
                                           border-radius:0.25rem;
                                           font-size:var(--app-played-list-font-size);
                                           font-family:var(--app-played-list-font-family);
                                           line-height:1.4em;
                                           max-height:calc(var(--app-played-list-max-lines) * 1.4em + 1rem);
                                           overflow: hidden;
                                           display: -webkit-box;
                                           -webkit-box-orient: vertical;
                                           -webkit-line-clamp: var(--app-played-list-max-lines);
                                       }
                                       .winner-modal{
                                           position:fixed;
                                           inset:0;
                                           z-index:1000;
                                       }
                                       .winner-modal-backdrop{
                                           position:absolute;
                                           inset:0;
                                           background:rgba(0, 0, 0, 0.65);
                                       }
                                       .winner-modal-content{
                                           position:relative;
                                           width:min(90vw, 36rem);
                                           margin:10vh auto 0;
                                           padding:1.5rem;
                                           border-radius:0.75rem;
                                           background:rgba(24, 24, 24, 0.95);
                                           color:var(--app-text-color);
                                           box-shadow:0 0 30px rgba(255, 225, 120, 0.35);
                                           text-align:center;
                                           overflow:hidden;
                                           animation:winner-pop 280ms ease-out;
                                       }
                                       .winner-main-line{
                                           margin:0.75rem 0 0.5rem;
                                           font-size:1.1rem;
                                           font-weight:700;
                                       }
                                       .winner-details{
                                           margin:0 0 1rem;
                                           font-size:0.95rem;
                                           opacity:0.95;
                                       }
                                       .winner-confetti{
                                           position:absolute;
                                           inset:0;
                                           pointer-events:none;
                                           overflow:hidden;
                                       }
                                       .winner-confetti-piece{
                                           position:absolute;
                                           top:-10%;
                                           width:8px;
                                           height:14px;
                                           border-radius:2px;
                                           opacity:0.95;
                                           animation:confetti-fall 1.6s linear forwards;
                                       }
                                       @keyframes winner-pop{
                                           0% { transform:scale(0.92); opacity:0; }
                                           100% { transform:scale(1); opacity:1; }
                                       }
                                       @keyframes confetti-fall{
                                           0% { transform:translateY(-10%) rotate(0deg); }
                                           100% { transform:translateY(115vh) rotate(540deg); opacity:0.85; }
                                       }
                                       @media (max-width: 1200px) {
                                           #wheelContainer{
                                               width:clamp(6.25rem, min(60vmin, 85%), 40rem);
                                               height:clamp(6.25rem, min(60vmin, 85%), 40rem);
                                           }
                                           #playedList{
                                               width:min(25rem, 35vw);
                                               min-width:15rem;
                                           }
                                       }
                                       @media (max-width: 900px) {
                                           #container{
                                               flex-direction: column;
                                           }
                                           #wheelSection{
                                               padding-top:0.5rem;
                                           }
                                           #wheelContents{
                                               padding-top:0.5rem;
                                           }
                                           #wheelContainer{
                                               width:clamp(6.25rem, min(50vmin, 80vw), 30rem);
                                               height:clamp(6.25rem, min(50vmin, 80vw), 30rem);
                                           }
                                           #resizeHandle{
                                               display:none;
                                           }
                                           #playedList{
                                               width:100%;
                                               min-width:100%;
                                               max-width:100%;
                                               height:auto;
                                               max-height:40vh;
                                               border-top:2px solid var(--app-resize-handle-bg);
                                           }
                                           #soungCounterSection{
                                               flex-wrap: wrap;
                                               flex-shrink: 0;
                                           }
                                           #soungCounterSection h3{
                                               font-size:1rem;
                                               margin:0.5rem 0;
                                               flex-shrink: 0;
                                           }
                                       }
                                       @media (max-width: 600px) {
                                           #wheelContainer{
                                               width:clamp(6.25rem, 85vw, 25rem);
                                               height:clamp(6.25rem, 85vw, 25rem);
                                           }
                                           #pointer{
                                               border-left:1rem solid transparent;
                                               border-right:1rem solid transparent;
                                               border-top:1.5rem solid var(--app-pointer-color);
                                           }
                                           button{
                                               padding:0.5rem 1.25rem;
                                               font-size:1rem;
                                           }
                                           #playedList{
                                               padding-left:0.75rem;
                                               padding-right:0.75rem;
                                           }
                                       }
                                       </style>
                                       </head>
                                       <body>

                                       <div id="container">
                                           <div id="wheelSection">
                                               <div id="wheelContents">
                                                   <div id="pointer"></div>
                                                   <div id="wheelContainer"></div>
                                               </div>
                                           </div>
                                           <div id="resizeHandle" style="display:none"></div>
                                           <div id="playedList" data-position="right">
                                               <div id="playedListTop">
                                                   <button id="collapseBtn" class="collapse-button" onclick="handleToggleCollapse()" title="Collapse/Expand Played List">
                                                       <span id="collapseIcon">&#9664;</span>
                                                   </button>
                                                   <div id="streamerSection">
                                                       <span id="streamerLabel">Waiting for Dashboard...</span>
                                                   </div>
                                               </div>
                                               <div id="playedListContent">
                                                   <div id="soungCounterSection">
                                                       <h3>Played Songs: <span id="playedCount">0</span></h3>
                                                       <h3>Queued Songs: <span id="availableCount">0</span></h3>
                                                   </div>
                                                   <ul id="playedSongsUl"></ul>
                                               </div>
                                           </div>
                                       </div>

                                       <div id="winnerModal" class="winner-modal" style="display:none">
                                           <div class="winner-modal-backdrop" onclick="handleCloseWinner()"></div>
                                           <div class="winner-modal-content" role="dialog" aria-modal="true" aria-labelledby="winnerTitle">
                                               <div id="winnerConfetti" class="winner-confetti"></div>
                                               <h2 id="winnerTitle">Winner</h2>
                                               <p id="winnerMainLine" class="winner-main-line"></p>
                                               <p id="winnerDetails" class="winner-details"></p>
                                               <button class="small-button" onclick="handleCloseWinner()">Close</button>
                                           </div>
                                       </div>

                                       <script src="https://cdn.jsdelivr.net/npm/spin-wheel@5.0.2/dist/spin-wheel-iife.js"></script>
                                       <script>
                                       window.SpinnerInterop = (function () {
                                           let _wheel = null
                                           let _resizeTimeout = null

                                           return {
                                               createWheel(items, colors) {
                                                   const container = document.getElementById('wheelContainer')
                                                   if (!container) return
                                                   if (_wheel) { _wheel.remove(); _wheel = null }
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

                                               setupResizeObserver() {
                                                   const container = document.getElementById('wheelContainer')
                                                   if (!container || !window.ResizeObserver) return
                                                   new ResizeObserver(() => {
                                                       if (_wheel) {
                                                           clearTimeout(_resizeTimeout)
                                                           _resizeTimeout = setTimeout(() => {
                                                               if (_wheel) {
                                                                   const items = _wheel.items
                                                                   const colors = _wheel.itemBackgroundColors
                                                                   _wheel.remove()
                                                                   _wheel = new spinWheel.Wheel(container, {
                                                                       items,
                                                                       itemBackgroundColors: colors || [],
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

                                               applyPlayedListPosition(position) {
                                                   const container = document.getElementById('container')
                                                   const icon = document.getElementById('collapseIcon')
                                                   if (!container || !icon) return
                                                   if ((position || 'right').toLowerCase() === 'left') {
                                                       container.classList.add('played-list-left')
                                                       icon.innerText = '\u25BA'
                                                   } else {
                                                       container.classList.remove('played-list-left')
                                                       icon.innerText = '\u25C4'
                                                   }
                                                   const pl = document.getElementById('playedList')
                                                   if (pl) pl.dataset.position = (position || 'right').toLowerCase()
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
                                                       el.classList.add('collapsed')
                                                       icon.innerText = pos === 'left' ? '\u25C4' : '\u25BA'
                                                   } else {
                                                       el.classList.remove('collapsed')
                                                       icon.innerText = pos === 'left' ? '\u25BA' : '\u25C4'
                                                   }
                                               },

                                               setPlayedListWidth(width, minWidth) {
                                                   const el = document.getElementById('playedList')
                                                   if (!el) return
                                                   if (width) el.style.width = width
                                                   if (minWidth) el.style.minWidth = minWidth
                                               }
                                           }
                                       })()
                                       </script>
                                       <script>
                                       const _overlayState = { config: null, collapsed: false }

                                       function connectSSE() {
                                           const es = new EventSource('/overlay/events')

                                           es.addEventListener('init_state', e => {
                                               const data = JSON.parse(e.data)
                                               _overlayState.config = data.config
                                               if (data.config) {
                                                   SpinnerInterop.applyTheme(data.config.colors, data.config.playedList)
                                                   SpinnerInterop.applyBackground(data.config.background)
                                                   SpinnerInterop.applyPlayedListPosition(data.config.songList && data.config.songList.playedListPosition)
                                               }
                                               updateSongs(data)
                                           })

                                           es.addEventListener('update_songs', e => {
                                               updateSongs(JSON.parse(e.data))
                                           })

                                           es.addEventListener('spin_command', e => {
                                               const data = JSON.parse(e.data)
                                               SpinnerInterop.spinToItem(data.winnerIndex, data.duration)
                                               setTimeout(() => {
                                                   document.getElementById('winnerMainLine').textContent = data.mainLine
                                                   document.getElementById('winnerDetails').textContent = data.details
                                                   document.getElementById('winnerModal').style.display = 'block'
                                                   if (_overlayState.config) SpinnerInterop.runConfetti(_overlayState.config.wheelColors)
                                               }, data.duration + 100)
                                           })

                                           es.addEventListener('close_winner', () => {
                                               document.getElementById('winnerModal').style.display = 'none'
                                           })

                                           es.addEventListener('set_wheel_visible', e => {
                                               SpinnerInterop.setWheelVisible(JSON.parse(e.data).visible)
                                           })

                                           es.addEventListener('set_collapse', e => {
                                               const data = JSON.parse(e.data)
                                               _overlayState.collapsed = data.collapsed
                                               const pos = _overlayState.config && _overlayState.config.songList && _overlayState.config.songList.playedListPosition
                                               SpinnerInterop.setPlayedListCollapsed(data.collapsed, pos)
                                           })

                                           es.addEventListener('set_played_list_width', e => {
                                               const data = JSON.parse(e.data)
                                               SpinnerInterop.setPlayedListWidth(data.width, data.minWidth)
                                           })

                                           es.onerror = () => {
                                               es.close()
                                               setTimeout(connectSSE, 3000)
                                           }
                                       }

                                       function updateSongs(data) {
                                           const cfg = _overlayState.config
                                           SpinnerInterop.createWheel(data.wheelItems || [], cfg && cfg.wheelColors || [])
                                           document.getElementById('playedCount').textContent = data.playedCount != null ? data.playedCount : 0
                                           document.getElementById('availableCount').textContent = data.availableCount != null ? data.availableCount : 0
                                           if (data.streamer) document.getElementById('streamerLabel').textContent = data.streamer
                                           const ul = document.getElementById('playedSongsUl')
                                           ul.innerHTML = ''
                                           ;(data.playedTexts || []).forEach(text => {
                                               const li = document.createElement('li')
                                               li.textContent = text
                                               ul.appendChild(li)
                                           })
                                       }

                                       function handleToggleCollapse() {
                                           _overlayState.collapsed = !_overlayState.collapsed
                                           const pos = _overlayState.config && _overlayState.config.songList && _overlayState.config.songList.playedListPosition
                                           SpinnerInterop.setPlayedListCollapsed(_overlayState.collapsed, pos)
                                       }

                                       function handleCloseWinner() {
                                           document.getElementById('winnerModal').style.display = 'none'
                                       }

                                       window.addEventListener('load', () => {
                                           SpinnerInterop.createWheel([{ label: 'Waiting for Dashboard...' }], [])
                                           SpinnerInterop.setupResizeObserver()
                                           connectSSE()
                                       })
                                       </script>
                                       </body>
                                       </html>
                                       """;

    private readonly TwitchAuthService _auth;
    private readonly CancellationTokenSource _cts = new();
    private readonly OverlayStateService _overlay;
    private HttpListener? _listener;

    public LocalOverlayServer(OverlayStateService overlay, TwitchAuthService auth)
    {
        _overlay = overlay;
        _auth = auth;
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        try
        {
            _listener?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            // ignored
        }

        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_overlay.Port}/");
        _listener.Prefixes.Add("http://localhost:3000/auth/");
        try
        {
            _listener.Start();
            _ = ProcessRequestsAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OverlayServer] Failed to start on port {_overlay.Port}: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        try
        {
            _listener?.Stop();
        }
        catch (ObjectDisposedException ex) { _ = ex; }

        return Task.CompletedTask;
    }

    private async Task ProcessRequestsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(context, ct), CancellationToken.None);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken ct)
    {
        var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? "";
        try
        {
            switch (path)
            {
                case "" or "/":
                    context.Response.Redirect("/overlay");
                    context.Response.Close();
                    break;
                case "/overlay":
                    await ServeHtmlAsync(context);
                    break;
                case "/overlay/events":
                    await ServeSSEAsync(context, ct);
                    break;
                case "/auth/callback":
                    await ServeAuthCallbackAsync(context);
                    break;
                default:
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    break;
            }
        }
        catch
        {
            try
            {
                context.Response.Abort();
            }
            catch (Exception ex) { _ = ex; }
        }
    }

    private static async Task ServeHtmlAsync(HttpListenerContext context)
    {
        var bytes = Encoding.UTF8.GetBytes(OverlayHtml);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private async Task ServeSSEAsync(HttpListenerContext context, CancellationToken ct)
    {
        context.Response.ContentType = "text/event-stream";
        context.Response.AddHeader("Cache-Control", "no-cache");
        context.Response.AddHeader("X-Accel-Buffering", "no");
        context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        context.Response.SendChunked = true;

        await using var writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8, leaveOpen: true);
        writer.AutoFlush = true;
        try
        {
            await foreach (var msg in _overlay.SubscribeAsync(ct))
                await writer.WriteAsync(msg);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch (Exception ex) { _ = ex; }
        }
    }

    private async Task ServeAuthCallbackAsync(HttpListenerContext context)
    {
        var query = context.Request.QueryString;
        var code = query["code"];
        var state = query["state"];
        var error = query["error"];

        string message;
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            message = "Login failed or was cancelled. You can close this tab.";
        }
        else
        {
            var ok = await _auth.CompleteOAuthAsync(code, state);
            message = ok ? "Login successful! You can close this tab." : "Login failed. You can close this tab.";
        }

        var html = "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
                   "<style>body{font-family:sans-serif;display:flex;align-items:center;justify-content:center;height:100vh;margin:0;background:#0e0e10;color:#efeff1}h2{color:#9147ff}</style>" +
                   $"</head><body><h2>{message}</h2></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }
}