@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo MonkeySpeak Updater
echo ==================

set "TARGET_DIR=%~1"
set "UPDATE_DIR=%~2"
set "EXECUTABLE=%~3"
set "PROCESS_ID=%~4"

echo Waiting for process %PROCESS_ID% to close...
timeout /t 2 /nobreak >nul

:wait_loop
tasklist /fi "PID eq %PROCESS_ID%" | find "%PROCESS_ID%" >nul
if not errorlevel 1 (
    timeout /t 1 /nobreak >nul
    goto wait_loop
)

echo Updating files from %UPDATE_DIR% to %TARGET_DIR%...
xcopy /y /e /i "%UPDATE_DIR%\*" "%TARGET_DIR%"

echo Launching %EXECUTABLE%...
start "" "%TARGET_DIR%\%EXECUTABLE%"

echo Cleaning up...
rd /s /q "%UPDATE_DIR%"

echo Update completed successfully!
timeout /t 3 /nobreak >nul
endlocal