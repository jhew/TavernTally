# Development restart script for TavernTally
Write-Host "🔄 Restarting TavernTally..." -ForegroundColor Green

# Build the solution first
Write-Host "Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build TavernTally.sln --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green

# Create development restart flag
$flagPath = "$env:TEMP\TavernTally.DevRestart.flag"
Set-Content -Path $flagPath -Value (Get-Date).ToString()

# Start the new version with development restart flag
Write-Host "Starting updated version..." -ForegroundColor Yellow
$exePath = "src\TavernTally\bin\Debug\net8.0-windows\TavernTally.exe"

if (Test-Path $exePath) {
    Start-Process $exePath -ArgumentList "--dev-restart"
    Write-Host "✅ TavernTally restart initiated!" -ForegroundColor Green
    Write-Host "   The new instance will handle killing the old one." -ForegroundColor Gray
} else {
    Write-Host "❌ Executable not found: $exePath" -ForegroundColor Red
    exit 1
}
