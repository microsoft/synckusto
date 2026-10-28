# README screenshot

The README embeds the root `screenshot.png`. Refresh it when requested or when visible main-window changes make the image stale (for example, picker layout, toolbar, comparison tree, diff display, or build label changes).

On Windows, with PowerShell 7.2 or newer, a desktop session, and an SDK compatible with `global.json` (`10.0.402` or a newer servicing patch in the `10.0.4xx` feature band), run from the repository root:

```powershell
pwsh -NoProfile -File .\scripts\Update-ReadmeScreenshot.ps1
```

The command builds the current code and captures the real form with fixed fictional data. It does not connect to Kusto or read/write saved user settings. Keep those guarantees: never substitute real credentials, connections, schemas, or a full-desktop capture. No manual app setup is needed.

Inspect the generated image before including `screenshot.png` with the relevant UI changes. Confirm the current build label, both pickers, expanded comparison, checked `CurrentTime` function, and red/yellow diff are visible. If capture is unavailable or fails, report that limitation rather than fabricating an image or claiming it was refreshed. Do not edit the image by hand or add automatic CI updates.

See `CONTRIBUTING.md` for prerequisites and capture details.
