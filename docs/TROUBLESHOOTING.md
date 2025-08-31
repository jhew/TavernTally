# 🛠️ TavernTally Troubleshooting

### Overlay not showing
- Make sure **Hearthstone is running in English** (log parsing is language-dependent in v1).
- Check that **logs are enabled** in `options.txt`. (Hearthstone Deck Tracker usually sets this for you.)
- TavernTally hides itself unless **Hearthstone is the active window**.
- Try restarting both Hearthstone and TavernTally.

### Labels misaligned
- Run **Tray → Align Overlay** to nudge overlay by +10,+10 pixels.
- Edit `config.json` at  
  `%LOCALAPPDATA%\TavernTally\config.json`  
  and tweak `OffsetX` / `OffsetY` manually (positive = shift right/down).
- If you use **DPI scaling** (125%, 150%, etc.), run Windows at 100% scaling or use the alignment tool.

### Hotkeys not working
- Some other apps may intercept **F8** or `Ctrl+=` / `Ctrl+-`.  
- Try running TavernTally as normal user (not admin).  
- Change or disable conflicting shortcuts in `config.json` (future version will allow custom hotkeys via UI).

### Update check says “disabled”
- Open `%LOCALAPPDATA%\TavernTally\config.json`  
- Set `"UpdateJsonUrl": "https://yourcdn/releases.json"`  
- Restart the app and use Tray → *Check for Updates*.

### Crashes or errors
- Check logs at:  
  `%LOCALAPPDATA%\TavernTally\logs\taverntally.log`  
- Attach logs when filing an issue.

---

If problems persist, [open a GitHub Issue](../issues).
