@echo off
setlocal

:: ── Check for Node.js ───────────────────────────────────────────────────────
where node >nul 2>nul
if %errorlevel% neq 0 (
    echo.
    echo  Node.js is not installed.
    echo.
    echo  Please download and install it from:
    echo    https://nodejs.org/en/download
    echo.
    echo  After installing, restart your PC, then run this file again.
    echo.
    start https://nodejs.org/en/download
    pause
    exit /b 1
)

:: ── Install dependencies on first run ───────────────────────────────────────
if not exist "node_modules" (
    echo  Installing dependencies ^(first time only^)...
    echo.
    call npm install
    if %errorlevel% neq 0 (
        echo.
        echo  Failed to install dependencies.
        echo  Check your internet connection and try again.
        echo.
        pause
        exit /b 1
    )
    echo.
)

:: ── Start server and open browser ───────────────────────────────────────────
echo.
echo  Song Spinner is starting...
echo.
echo   Overlay ^(Browser Source^):  http://localhost:3000/
echo   Control panel ^(Dock^):      http://localhost:3000/control
echo   Setup ^(Twitch / AutoPlay^): http://localhost:3000/setup
echo.
echo  Press Ctrl+C to stop.
echo.

:: Open the control panel in the default browser after the server has a moment to start
start /b cmd /c "timeout /t 2 /nobreak >nul && start http://localhost:3000/control"

node server.js
pause
