# Stop any running processes
Get-Process | Where-Object { $_.ProcessName -eq "TavernTally" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Clean build directories
if (Test-Path "src\TavernTally\bin") { Remove-Item "src\TavernTally\bin" -Recurse -Force }
if (Test-Path "src\TavernTally\obj") { Remove-Item "src\TavernTally\obj" -Recurse -Force }
if (Test-Path "src\Updater\bin") { Remove-Item "src\Updater\bin" -Recurse -Force }
if (Test-Path "src\Updater\obj") { Remove-Item "src\Updater\obj" -Recurse -Force }

# Clean and rebuild
dotnet clean
dotnet build
