# Stop any running processes
Get-Process | Where-Object { $_.ProcessName -eq "TavernTally.App" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Clean build directories
if (Test-Path "src\TavernTally.App\bin") { Remove-Item "src\TavernTally.App\bin" -Recurse -Force }
if (Test-Path "src\TavernTally.App\obj") { Remove-Item "src\TavernTally.App\obj" -Recurse -Force }
if (Test-Path "src\Updater\bin") { Remove-Item "src\Updater\bin" -Recurse -Force }
if (Test-Path "src\Updater\obj") { Remove-Item "src\Updater\obj" -Recurse -Force }

# Clean and rebuild
dotnet clean
dotnet build
