@echo off
title Hearing Test Mobile Web Server
cd /d "%~dp0"
echo ====================================================
echo   Starting Mobile Web Server (Port 8080)...
echo ====================================================
node mobile-web\server.js
if errorlevel 1 (
    echo.
    echo [ERROR] Failed to start server. Please make sure Node.js is installed.
)
pause
