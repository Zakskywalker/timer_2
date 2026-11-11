# Time Recorder 2.0 (Timer 2.0)

<p align="center">
  <img src="assets/splash.jpg" alt="Hand of Glory Games and Software Splash Logo" width="240" />
</p>

**Hand of Glory Games and Software** • **Author:** Zak Millikin (Zakskywalker M_A.P.) • **License:** [GNU GPL v3.0](https://www.gnu.org/licenses/gpl-3.0.html)

A modern, high-precision .NET Windows Forms time recording and tracking system featuring fault-tolerant auto-healing logging, notification tray integration, dynamic theme switching, dual-table MySQL sync and compare panel, responsive stretchable and "phone bar" docking layout, Chrome activity recording, and interactive HTML reporting with sticky summaries.

Licensed under the **GNU General Public License v3 (GPLv3)**.

---

## What's New & Enhancements

### 1. Tightened Spacing & Compact RichTextBox
- **Optimized Console Height**: Reduced the rich text box height to ~80px to eliminate unnecessary blank vertical space, ensuring all 3 log lines fit snug and clean.
- **Tightened Vertical Padding**: Cleaned up the spacing between the directory box, checkbox, buttons, console, and copy bar.

### 2. Stretchable & Dynamic "Phone Bar" Layout
- **Full Stretchability**: The window can be expanded wider or taller, and all internal controls adapt smoothly.
- **Phone Bar Mode (Width < 400px)**:
  - Automatically switches into a narrow, vertical phone-bar widget.
  - Buttons reorganize into a balanced 2x2 grid (`Start/Stop`, `Total` on row 1; `Add/Modify Time`, `Log Viewer` on row 2).
  - Directory text box, log console, and copy controls adapt to fit the narrow profile without clipping.
  - `MinimumSize` set to `280 x 400` so it can be docked neatly as a slim bar beside any work window.

### 3. Database Sync, Compare & User Submit Panel
- **Dual Table Variables & Methods**:
  - `TimerLogsTable`: default `timer_sessions` (stores structured timer runs, start/stop, durations, warnings).
  - `SystemLogsTable`: default `timer_system_logs` (stores system and error event logs).
- **Retrieve from DB**: Fetch raw records from both tables.
- **Compare to Current DB**:
  - Identifies differences between local logs and database records.
  - Color-coded sync statuses:
    - 🟢 `To Upload` (Local sessions missing in DB)
    - 🟡 `Modified` (Session exists in both, but timestamps or durations differ)
    - ⚪ `In Sync` (Already synchronized)
    - 🔵 `In DB Only` (Exists in database only)
- **User Submit**:
  - Select individual rows or all rows using checkboxes.
  - Submit chosen records into the database table with full audit confirmation.
- **Relative Sized Panel**: Opens as a modal dialog matching the original timer's form factor.

### 4. GNU License (GPLv3)
- Full license included in `LICENSE` file.
- Viewable from `File > About & License (GPLv3)`.

### 5. Windows 11 Compatibility & Installer
- **Self-Contained Standalone Publish**: Built for `win-x64` with all .NET Windows Desktop runtimes and native dependencies embedded into a single portable binary (`publish/win11-x64/Timer2.App.exe`). Runs on any Windows 11 machine without installing extra dependencies.
- **Inno Setup Script (`installer/setup.iss`)**: Generates a standard Windows 11 modern wizard installer (`dist/TimeRecorder_2.0_Win11_Setup.exe`).
- **PowerShell Windows 11 Installer (`installer/install_windows11.ps1`)**: Turnkey script that installs the application into `%LOCALAPPDATA%\Programs\TimeRecorder`, generates Start Menu & Desktop shortcuts with the custom clock icon, and registers the uninstaller with Windows 11 Installed Apps (`Add/Remove Programs`).

### 6. Google Chrome Activity Logging
- Open **Google Chrome Activity** below the main timer console to start or stop recording independently of the work timer, or capture the current Chrome window title once.
- Activity is written to separate daily text files under `ChromeActivity` in the configured log directory. Closing the activity panel does not stop recording; the log day selector is disabled while recording.
- Capture is process-only: it records the foreground Chrome window/tab title when observed, not URLs or background-tab changes. The Chrome correlation HTML report is available from **Log Viewer** and compares observed intervals with timer sessions; intervals without matching timer timestamps are marked unknown.

---

## Directory Structure

```
timer_2_0/
├── Timer20.slnx
├── LICENSE                    # GNU General Public License v3
├── README.md
├── time.txt, log.txt, etc.    # Working and imported logs
├── src/
│   ├── Timer2.Core/           # Core calculations, database, storage & HTML export
│   │   ├── Calculations/      # TimeCalculator, decimal formatting, H:P / H:M
│   │   ├── Database/          # IDataConnector, MySqlDataConnector (Dual tables & Compare)
│   │   ├── Export/            # IHtmlTemplate, Dark/Light templates, HTML report generators
│   │   ├── Models/            # TimeEntry, DaySummary, DbComparisonModels, AppConfig, etc.
│   │   └── Storage/           # LogManager, ChromeActivityLog, LogParser, LogIndexManager, ConfigManager
│   └── Timer2.App/            # Windows Forms Project (.NET 10.0 Windows)
│       ├── Forms/             # MainForm, ChromeActivityForm, DbSyncComparisonForm, etc.
│       └── Services/          # Chrome process-window title recorder
│       ├── UI/                # ClockIconFactory, ThemeManager
├── tests/
    └── Timer2.Tests/          # Automated unit tests
├── installer/
│   ├── setup.iss              # Inno Setup Windows 11 Installer script
│   ├── install_windows11.ps1   # PowerShell automated Windows 11 installer
│   └── build_installer.ps1    # Distribution packager
└── publish/
    └── win11-x64/             # Self-contained Windows 11 single-file executable
```

---

## How to Run & Install

### 1. Build and launch the Setup Install Wizard:
Publish the standalone setup package (the application payload is published and embedded automatically):
```powershell
dotnet publish .\src\Timer2.Installer\Timer2.Installer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\dist\setup
Copy-Item .\dist\setup\Timer2.Installer.exe .\dist\TimeRecorder_2.0_Setup.exe
```
Then run the generated installer:
```powershell
.\dist\TimeRecorder_2.0_Setup.exe
```
This runs the full **Setup Install Wizard UI**, which guides you through:
1. **Welcome Screen** with version & feature overview.
2. **GNU General Public License v3** agreement.
3. **Destination Folder Selection** with disk space verification.
4. **Additional Tasks Selection** (Desktop shortcut, Start Menu shortcut, Windows auto-start).
5. **Ready to Install** configuration summary.
6. **Animated Installation Progress Bar**.
7. **Completion Screen** with a one-click option to launch Time Recorder immediately.

### 2. Run Directly from Source:
```powershell
dotnet run --project timer_2_0/src/Timer2.App/Timer2.App.csproj
```

### 3. Alternative Silent/PowerShell Windows 11 Installer:
```powershell
powershell -ExecutionPolicy Bypass -File timer_2_0/installer/install_windows11.ps1
```

### Download the Windows Installer

The CI workflow runs the test suite and, on pushes to `main`, publishes a self-contained Windows x64 setup executable to GitHub Releases. Download the latest installer from the setup page:

<https://zakskywalker.github.io/timer_2/>

Download and open `TimeRecorder_2.0_Setup.exe` to install. The setup executable includes the application payload; do not download or open `Timer2.App.application` separately. Each push to `main` publishes a new installer and app update. Installed standalone copies show a startup splash while checking GitHub Releases and automatically download and apply a newer app version when available.

The workflow also keeps publishing the ClickOnce deployment for existing ClickOnce installations. Those installations check for updates in the background after launch. Timer data stays in the configured log folder (by default, `%USERPROFILE%\Documents\TimerLogs`) and is not included in the installer.


---

## Versioning & Pre-Submit Checklist (Before Pushing to GitHub)

### 1. Versioning Architecture
- **Current Release Line:** `2.0.0` (`Major.Minor.Patch`), configured centrally in the project files.
- **Automated ClickOnce & GitHub Releases Build Version:** `2.0.<GITHUB_RUN_NUMBER>.<GITHUB_RUN_ATTEMPT>`.
  - On every push to `main`, `.github/workflows/clickonce-pages.yml` builds, publishes the ClickOnce site, builds the standalone self-extracting Windows installer (`TimeRecorder_2.0_Setup.exe`), publishes an automated GitHub Release, and writes the deployment metadata to `clickonce/version.json`.
  - Installed standalone copies show a startup splash while checking GitHub Releases and automatically download and apply a newer app version when available.
- **Files to Update When Bumping `Major.Minor.Patch` (e.g., `2.0.0` -> `2.1.0`):**
  1. `src/Timer2.App/Timer2.App.csproj` and `src/Timer2.Installer/Timer2.Installer.csproj` — `<Version>2.1.0</Version>`
  2. `.github/workflows/clickonce-pages.yml` — `$version = "2.1.$env:GITHUB_RUN_NUMBER.$env:GITHUB_RUN_ATTEMPT"`
  3. `docs/version.json` — `"version": "2.1.0"`
  4. `.github/pages/index.html` and `docs/index.html` version badges

### 2. Pre-Push / Pre-Submit Checklist
Before committing and pushing changes to GitHub (`origin/main`), run through these steps:

```powershell
# Step 1: Run the full unit test suite in Release mode and ensure all 17 tests pass
dotnet test Timer20.slnx --configuration Release

# Step 2: Build the standalone installer payload and verify it packages successfully
dotnet publish src/Timer2.App/Timer2.App.csproj -c Release -r win-x64 --self-contained true
dotnet publish src/Timer2.Installer/Timer2.Installer.csproj -c Release -r win-x64 --self-contained true

# Step 3: Inspect git status to ensure no log files (time.txt, log.txt, settings.txt) are staged
git status
```

- **Guard Protected Log Data:** Under no circumstances should user runtime files (`time.txt`, `log.txt`, `log2.txt`, `changes.txt`, `temp.txt`, `settings.txt`) or personal log directories (`TimerLogs`) be staged or checked into Git. The workflow has built-in verification gates that will abort the publish run if any timer log data is detected in the payload.
- **Push and Verify CI/CD:**
  ```powershell
  git add -A
  git commit -m "Describe your change"
  git push origin main
  gh run list --repo Zakskywalker/timer_2 --limit 3
  ```

---

## Credits & Attribution

- **Creator & Lead Architect:** Zak Millikin (**Zakskywalker M_A.P.**) — *Hand of Glory Games and Software*
- **AI Pair Programmers:** Google DeepMind **Antigravity** (*Claude Opus* & *Gemini*)
- **Splash Screen Artwork:** [**Perchance AI (Cursed)**](https://perchance.org/cursed-ai)
- **License:** [GNU General Public License v3.0 (GPL-3.0-or-later)](https://www.gnu.org/licenses/gpl-3.0.html)
