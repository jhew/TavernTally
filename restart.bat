@echo off
echo Restarting TavernTally...

REM Stop existing instances
taskkill /F /IM TavernTally.exe >nul 2>&1

REM Wait a moment
timeout /t 1 /nobreak >nul

REM Build the solution
echo Building...
dotnet build TavernTally.sln --verbosity quiet

REM Start the new version
echo Starting updated version...
start "TavernTally" "src\TavernTally\bin\Debug\net8.0-windows\TavernTally.exe"

echo ✅ TavernTally restarted!
