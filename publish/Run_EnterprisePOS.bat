@echo off
title Enterprise POS - Offline Desktop System
cd /d "C:\Users\SMBK\EnterprisePOS.Desktop\publish_desktop"

start "" /b dotnet EnterprisePOS.dll
ping 127.0.0.1 -n 3 >nul

start "" EnterprisePOS.Desktop.exe
exit
