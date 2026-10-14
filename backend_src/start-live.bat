@echo off
title Enterprise POS - Live Server
echo ========================================================
echo        STARTING ENTERPRISE POS & CLOUDFLARE TUNNEL      
echo ========================================================
echo.

:: 1. Start Enterprise POS Web App
echo Starting ASP.NET Core Application...
start "EnterprisePOS Web App" /min dotnet run --project "%~dp0EnterprisePOS.csproj" --no-build --urls "http://localhost:5281"

:: 2. Wait 3 seconds for app to initialize
timeout /t 3 /nobreak >nul

:: 3. Launch Cloudflare Tunnel
echo Launching Cloudflare Tunnel...
echo.
echo Look for the https://*.trycloudflare.com link below:
echo --------------------------------------------------------
"C:\Program Files (x86)\cloudflared\cloudflared.exe" tunnel --url http://localhost:5281

pause
