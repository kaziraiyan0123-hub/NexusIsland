@echo off
title NEXUS ISLAND Launcher
echo ========================================================
echo        ✦ NEXUS ISLAND — Windows 11 Desktop Companion
echo ========================================================
echo.
echo Starting NEXUS ISLAND...
echo Look at the top-center of your screen!
echo Global Shortcut: Press [ Ctrl + Shift + Space ] to toggle.
echo.

set "PATH=C:\Program Files\dotnet;%PATH%"
cd /d "%~dp0bin\Release\net8.0-windows10.0.19041.0"

start "" "NexusIsland.exe"

echo.
echo NEXUS ISLAND is now running!
timeout /t 2 >nul
exit
