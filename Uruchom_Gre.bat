@echo off
title Conglomerate Tycoon 2
color 0B
echo ===================================================
echo   URUCHAMIANIE GRY: CONGLOMERATE TYCOON 2
echo ===================================================
echo.
echo Kompilacja i startowanie gry (dotnet run)...
echo.
dotnet run
if %ERRORLEVEL% neq 0 (
    echo.
    echo [BLAD] Nie udalo sie uruchomic gry. 
    echo Upewnij sie, ze posiadasz zainstalowane srodowisko .NET SDK.
    echo.
    pause
)
