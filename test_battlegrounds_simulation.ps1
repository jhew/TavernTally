# Test script to simulate real Battlegrounds log entries
$logPath = "$env:USERPROFILE\AppData\Local\Blizzard\Hearthstone\Logs\Power.log"

Write-Host "Testing TavernTally Battlegrounds log parsing..."
Write-Host "Adding simulated log entries to: $logPath"

# Clear the log and start fresh
Write-Host "Clearing existing log..."
"" | Out-File -FilePath $logPath -Encoding UTF8

Start-Sleep -Seconds 1

# 1. Simulate game creation
Write-Host "1. Simulating game creation..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - CREATE_GAME" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 1

# 2. Simulate Battlegrounds mode detection  
Write-Host "2. Simulating Battlegrounds mode..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - GAME_TYPE_BATTLEGROUNDS" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 1

# 3. Simulate game starting
Write-Host "3. Simulating game start..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - TAG_CHANGE Entity=[GameEntity id=1] tag=STATE value=RUNNING" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 2

# 4. Simulate card purchase (from shop to hand)
Write-Host "4. Simulating card purchase..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Alleycat id=25 zone=FRIENDLY_SECRET zonePos=1 cardId=CFM_315 player=2] zone from FRIENDLY_SECRET -> HAND" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 2

# 5. Simulate another card purchase
Write-Host "5. Simulating another card purchase..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Murloc Tidehunter id=26 zone=FRIENDLY_SECRET zonePos=2 cardId=EX1_506 player=2] zone from FRIENDLY_SECRET -> HAND" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 2

# 6. Simulate playing a minion (hand to board)
Write-Host "6. Simulating playing a minion..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Alleycat id=25 zone=HAND zonePos=1 cardId=CFM_315 player=2] zone from HAND -> PLAY" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 2

# 7. Simulate selling a card (hand to graveyard)
Write-Host "7. Simulating selling a card..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Murloc Tidehunter id=26 zone=HAND zonePos=1 cardId=EX1_506 player=2] zone from HAND -> GRAVEYARD" | Out-File -FilePath $logPath -Append -Encoding UTF8

Start-Sleep -Seconds 2

# 8. Simulate BACON (Battlegrounds) specific events
Write-Host "8. Simulating BACON events..."
$timestamp = Get-Date -Format "HH:mm:ss.fffffff"
"D $timestamp GameState.DebugPrintPower() - TAG_CHANGE Entity=[BACON_SHOP id=30] tag=BACON_MINIONS_IN_TAVERN value=3" | Out-File -FilePath $logPath -Append -Encoding UTF8

Write-Host ""
Write-Host "Test simulation complete!"
Write-Host "Check your TavernTally overlay - it should now show card count changes."
Write-Host "The overlay should show:"
Write-Host "- Hand count changes (purchases/sales)"
Write-Host "- Board count changes (playing minions)"
Write-Host "- Shop count detection"
Write-Host ""
Write-Host "If the overlay doesn't update, there may be an issue with log parsing."
