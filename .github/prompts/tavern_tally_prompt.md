# .prompt.md — TavernTally (Refined)

**Purpose:** Align contributors and maintainers on what TavernTally is, isn’t, and how we install/run it alongside Hearthstone—fast, safe, streamer‑friendly.

---

## TL;DR
- **What:** Lightweight, transparent, click‑through overlay that labels visible zones in **Hearthstone Battlegrounds** (shop/board/hand). No game assistance.
- **Why:** Improve viewer comprehension on streams and group watch sessions.
- **How:** User‑land WPF app that snaps to the Hearthstone window; per‑user MSI; optional first‑run calibration; optional auto‑update.
- **Non‑Goals:** No stats, no predicted odds, no hidden info, no automation, no memory scanning.

---

## One‑liner
TavernTally is a safe, click‑through overlay for **Hearthstone Battlegrounds** that adds simple labels for the tavern row, your board slots, and your hand—pure UI clarity, zero competitive advantage.

---

## Audience & Use Cases
- **Twitch / YouTube streamers**: let viewers follow the board state more easily.
- **Discord watch parties**: shared language for quick callouts.
- **Players / casters**: consistent labeling without revealing hidden info.

---

## Core Principles
1. **Safety first** — no memory reads, no injection, no automation. Visible‑only UI aid. Optionally parses `Power.log` conservatively for alignment/visibility state.
2. **Minimalism** — one clear feature; resist scope creep.
3. **Stream‑friendly** — readable at 1080p/1440p, subtle design, no clutter.
4. **Zero friction** — simple per‑user install, clean uninstall, no admin.
5. **Privacy by default** — no telemetry; network calls only on explicit *Check for Updates* (or optional auto‑update, off by default).
6. **Performance** — target <3% CPU, <60 MB RAM while Hearthstone is foreground.
7. **Transparency** — overlay shows nothing the viewer can’t also see.

---

## Non‑Goals
- No stats/odds calculators or deck trackers.
- No secret info, no automation, no memory scanning, no kernel drivers.
- No accounts, no always‑on network services.

---

## Supported Platform (v1)
- Windows 10 (21H2+) and Windows 11 desktop.
- Hearthstone PC client (English UI first; localization later).
- Multi‑monitor & high‑DPI aware.

---

## Installation & Runtime Model (HDT‑style, minus their features)
**Goals:** frictionless install; “just runs alongside Hearthstone”; predictable updates; streamer trust.

- **Installer:**
  - Per‑user **MSI** (no admin) or self‑contained .NET 8 bundle.
  - Ships with required runtime (self‑contained publish) → no extra downloads.
  - Digital signature; aim for EV cert to minimize SmartScreen prompts.
  - Optional **winget** manifest for power users.

- **First‑run experience:**
  - Auto‑detect Hearthstone (Battle.net default paths; registry fallbacks). If not found, provide a *Browse…* option.
  - Offer **Streamer Mode** toggle (default: **on**).
  - **Calibration Wizard**: shows thin guides for *Shop / Board / Hand*; user nudges to match their screen; writes percent anchors to config.
  - "Test Overlay" button to preview transparency and click‑through.

- **Running alongside Hearthstone:**
  - Process watcher hooks into the **Hearthstone** window handle (no injection). Overlay **snaps** and **resizes** when the HS client rect changes.
  - **Click‑through** always; never blocks drag‑and‑drop.
  - Overlay **auto‑hides** when HS is not foreground (unless debug override). Resurfaces on focus restore.
  - Resilient to Battle.net relaunches and HS restarts; continues tracking without user action.

- **Updates:**
  - Default: **manual** *Check for Updates* (pulls signed manifest, MSI + SHA‑256). Optional: **auto‑update** (opt‑in only).
  - Release notes shown before update proceeds.

- **Uninstall:**
  - Removes binaries and shortcuts. Option to keep `config.json` for convenience.

- **Permissions & safety:**
  - Runs in user space; no admin, no drivers, no kernel hooks.
  - No persistent background services; exits cleanly from tray.

---

## UX & Features (v1)
- **Overlay**: transparent, always‑on‑top, click‑through; **snaps to HS window**.
- **Labels**:
  - **Tavern row** → `1..N`
  - **Board slots** → `A..G`
  - **Hand** → `1..10`
- **Hotkeys (default):**
  - `F8` — toggle overlay
  - `Ctrl+=` / `Ctrl+-` — scale labels
  - (optional) `F10` — toggle debug badge
- **Tray menu:** Toggle Overlay, Align Overlay (nudge), Open Settings, Check Updates, Exit.
- **Debug badge:** shows BG/FG state & counts; auto‑hidden in Streamer Mode.
- **Accessibility:**
  - Presets for light/dark/high‑contrast; color‑blind‑safe hues.
  - DPI scaling; font legibility targets @ 1080p/1440p.

---

## Configuration (stored at `%LOCALAPPDATA%/TavernTally/config.json`)
```jsonc
{
  "ShowOverlay": true,
  "UiScale": 1.0,
  "OffsetX": 0,
  "OffsetY": 0,
  "ShopYPct": 0.12,
  "BoardYPct": 0.63,
  "HandYPct": 0.92,
  "StreamerMode": true,
  "DebugAlwaysShowOverlay": false,
  "UpdateJsonUrl": "",
  "AutoUpdate": false,
  "Hotkeys": { "Toggle": "F8", "ScaleUp": "Ctrl+=", "ScaleDown": "Ctrl+-", "Debug": "F10" }
}
```

---

## Architecture
- **WPF (.NET 8)** → borderless, transparent overlay window; high‑DPI aware.
- **WindowTracker** → finds HS window, tracks client rect, handles DPI/resolution changes, multi‑monitor bounds.
- **LogTail + Parser (optional)** → conservative regex of `Power.log` for basic state (no memory access); fail‑closed.
- **Layout** → percent‑based anchors relative to HS window; additive pixel offsets for fine control.
- **HotkeyManager** → global hotkeys; conflict detection; user‑editable.
- **Tray** → NotifyIcon + context menu.
- **Updater** → manifest fetch, MSI download, SHA‑256 verification, signature check.
- **Logging** → Serilog → `%LOCALAPPDATA%/TavernTally/logs/taverntally.log`.

---

## Testing & QA (HDT‑inspired scenarios)
**Functional**
- Appears correctly at 1920×1080, 2560×1440, 3440×1440, 4K (scaled), fullscreen and windowed modes.
- Click‑through verified; no interference with card drag, discover, shop rolls.
- Hotkeys work while HS has focus; no conflicts with common streamer tools.
- Overlay hides on alt‑tab / HS minimize; reappears on restore.
- Snap/rescale works on HS resolution changes and windowed/fullscreen modes.

**Streaming**
- OBS/Streamlabs: verify **Game Capture** and **Window Capture**; ensure overlay is captured when intended (document recommended settings).
- Text legibility at 6–8 Mbps Twitch encode; no flicker with capture sources.

**Performance**
- CPU <3%, RAM <60 MB during match; no GPU spikes.
- Log tailing stays under negligible I/O.

**Reliability**
- Survives Battle.net relaunch; reconnects to HS window automatically.
- Settings persistence; idempotent first‑run wizard skips when already calibrated.

---

## Packaging & Release
- **Artifacts**: exe, MSI (per‑user), SHA‑256 checksum, release notes.
- **Signing**: strong‑name + Authenticode; EV cert if budget allows.
- **Distribution**: GitHub Releases (primary); winget manifest.
- **Docs**: `CHANGELOG.md`, `docs/obs_setup.md`, `docs/troubleshooting.md`, `docs/faq.md`.
- **Pre‑release gate**: test on clean Win10/Win11 VMs.

---

## Security & Privacy
- No telemetry. No outbound calls except explicit update checks (and optional auto‑update if enabled).
- Least‑privilege: user folder only; no admin; no services.
- Clear privacy note in README and first‑run wizard.

---

## Roadmap
- **v0.1**: MVP overlay, manual offsets, hotkeys, MSI.
- **v0.2**: Streamer Mode toggle; improved parser regex; initial settings UI.
- **v0.3**: Auto‑snap to HS window; multi‑monitor & DPI polishing.
- **v0.4**: First‑run calibration wizard; OBS setup guide.
- **v0.5**: Optional auto‑update; winget; signed releases (EV if feasible).
- **v0.6**: Localization (DE/FR/ES), theme presets.
- **v1.0**: Hardened updater, docs, polished UX.

---

## Contributor Rules
- Stay within Blizzard ToS: no hidden info, no automation, no memory scanning.
- Respect minimal overlay scope; PRs that add gameplay advantage are rejected.
- Managed .NET only; avoid native injection.
- Every PR includes: before/after screenshots or short clip + changelog entry.
- CI: GitHub Actions build + basic UI/behavior checks must pass.

---

## Open Decisions / Questions
**Design & UX**
1. Default **Streamer Mode** on first run? (Proposed: **yes**.)
2. Font & size range: Segoe UI Semibold, 24–48pt — alternatives?
3. Hotkey remapping UI in v1 or defer to v2?
4. Theme presets: light/dark/high‑contrast — which default?

**Release & Ops**
5. Update channel: GitHub Releases manifest vs CDN?
6. Keep `config.json` on uninstall? (Proposed: **yes**.)
7. Minimum Windows version enforcement: Win10 21H2+ & Win11 only?
8. EV code signing investment for trust cues?

---

