@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "PORT="
set "ROOT=%~dp0"

if not exist "%ROOT%index.html" (
    echo Ne znaishov index.html bilya tsoho failu.
    echo Zapuskai tsei fail z papky WebGL build.
    pause
    exit /b 1
)

if not exist "%ROOT%Build" (
    echo Ne znaishov papku Build bilya index.html.
    echo Tse ne skhozhe na povnyi Unity WebGL build.
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
echo Folder: %ROOT%
echo URL: http://127.0.0.1:%PORT%/
echo.
echo Zakryi tse vikno, shchob zupynyty server.
echo.

pushd "%ROOT%" || (
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
