@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "PORT="
set "ROOT=%~dp0"
set "WEBGL_DIR="

if exist "%ROOT%index.html" if exist "%ROOT%Build" set "WEBGL_DIR=%ROOT%"

if not defined WEBGL_DIR if exist "%ROOT%Builds\WebGL\index.html" if exist "%ROOT%Builds\WebGL\Build" set "WEBGL_DIR=%ROOT%Builds\WebGL"

if not defined WEBGL_DIR (
    for /f "delims=" %%I in ('dir /b /s "%ROOT%index.html" 2^>nul') do (
        if not defined WEBGL_DIR (
            if exist "%%~dpIBuild" set "WEBGL_DIR=%%~dpI"
        )
    )
)

if not defined WEBGL_DIR (
    echo Ne znaishov WebGL build.
    echo Poklady tsei fail u papku de ye index.html i papka Build,
    echo abo zroby v Unity: File ^> Build Settings ^> WebGL ^> Build.
    pause
    exit /b 1
)

for %%P in (8126 8127 8128 8129 8130 8131 8132 8133 8134 8135 8136) do (
    if not defined PORT (
        powershell -NoProfile -Command "try { $client = [Net.Sockets.TcpClient]::new('127.0.0.1', %%P); $client.Close(); exit 1 } catch { exit 0 }" >nul 2>nul
        if !errorlevel!==0 set "PORT=%%P"
    )
)

if not defined PORT (
    echo Ne znaishov vilnyi port 8126-8136.
    echo Zakryi stari vikna z WebGL serverom i zapusty tsei fail znovu.
    pause
    exit /b 1
)

echo Starting Tower Defense WebGL...
echo Folder: %WEBGL_DIR%
echo URL: http://127.0.0.1:%PORT%/
echo.
echo Zakryi tse vikno, shchob zupynyty server.
echo.

pushd "%WEBGL_DIR%" || (
    echo Ne vdalosia vidkryty papku WebGL build.
    pause
    exit /b 1
)

start "" powershell -NoProfile -WindowStyle Hidden -Command "Start-Sleep -Seconds 1; Start-Process 'http://127.0.0.1:%PORT%/'"

where py >nul 2>nul
if %errorlevel%==0 (
    py -3 webgl_server.py %PORT%
    popd
    exit /b
)

where python >nul 2>nul
if %errorlevel%==0 (
    python webgl_server.py %PORT%
    popd
    exit /b
)

where python3 >nul 2>nul
if %errorlevel%==0 (
    python3 webgl_server.py %PORT%
    popd
    exit /b
)

popd
echo Python ne znaideno. Vstanovy Python abo zapuskai cherez Unity Build And Run.
pause
exit /b 1
