# 🔒 TavernTally Security Policy

TavernTally is designed to be **safe** for use with Hearthstone Battlegrounds.

## Threat Model
- We only **read Hearthstone log files** (`Power.log` in `%LocalAppData%\Blizzard\Hearthstone\Logs`).
- We **never read or write game memory**.
- We **never inject DLLs** or hook input.
- We **never automate gameplay**.
- By default, we **do not send any telemetry or network requests**.  
  - The only network call is when you explicitly click *Check for Updates*, which downloads a signed MSI from the official release feed.
- Install is **per-user** in `%LocalAppData%\TavernTally\`. No administrator rights required.
- All release binaries/MSIs are **digitally signed**.

## Reporting a Vulnerability
If you discover a security issue:
1. Please **do not file a public issue immediately**.  
2. Email: `security@yourdomain.com` (replace with your address).  
3. We will confirm receipt within 48 hours and work on a fix.

## Supported Versions
We only support the **latest release**. Always update via the built-in updater or [GitHub Releases](../releases).

---

**Important:** TavernTally must remain within Blizzard’s Terms of Service. It is strictly an overlay/companion app that mirrors **visible game state only**. Any attempts to extend it into automation or hidden-info tracking would risk account action and are out of scope for this project.
