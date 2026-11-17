@echo off
rem Envoltorio de configurar-rutas.ps1 para poder ejecutarlo desde CMD o con doble clic.
rem Agrega -ExecutionPolicy Bypass (solo a ese proceso) y resuelve la ruta del .ps1
rem a partir de la ubicacion de este archivo, sin depender del directorio actual.
setlocal
set "script=%~dp0configurar-rutas.ps1"

powershell -NoProfile -ExecutionPolicy Bypass -File "%script%" %*
set "codigo=%ERRORLEVEL%"

if not "%codigo%"=="0" (
    echo.
    echo El script termino con codigo %codigo%.
    pause
)

exit /b %codigo%
