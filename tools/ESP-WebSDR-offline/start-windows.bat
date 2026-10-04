@echo off
setlocal
set "ROOT=%~dp0"

where py >nul 2>nul
if not errorlevel 1 (
  py -3 "%ROOT%tools\local_server.py" %*
  exit /b %errorlevel%
)

where python >nul 2>nul
if not errorlevel 1 (
  python "%ROOT%tools\local_server.py" %*
  exit /b %errorlevel%
)

echo Python 3 is required to start the local server.
echo Install Python 3, then run start-windows.bat again.
exit /b 1
