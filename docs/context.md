# Grave Zoom - project context

Working notes about how the mod is designed and how to work on it. The README covers usage; this file covers what you would otherwise have to learn by reading the code. Keep it free of personal paths, names and emails (the repo is public).

## Layout

| Path | What it is |
| --- | --- |
| `GraveZoom/` | Main plugin (net48). Never references the GK2 Mod Framework. |
| `GraveZoom.Framework/` | Optional bridge plugin (netstandard2.1). Registers Grave Zoom in GK2 Mod Framework's Mods menu. |
| `tests/GraveZoom.Tests/` | xUnit tests (net10.0). Link `ZoomMath.cs` and `ControllerBinding.cs` from the main project. |
| `tests/install.bats` | bats tests for `install.sh`. |
| `install.sh` | Linux / Steam Deck installer. Downloads the latest release zip. |
| `docs/description.md` | Nexus description source (BBCode). |
| `Directory.Build.props` | Shared build paths. Per-machine overrides go in the git-ignored `Directory.Build.local.props`. |

## How the zoom works

- The game computes the camera size in `CameraSystem.CalculateOrthographicSize` (vanilla = `height / (100 * pixelSize)`). Its only caller is `CameraSystem.OnResolutionChanged`.
- A Harmony postfix (`CameraSystemPatches.cs`) multiplies the result by `ZoomMath.OrthoScale(zoom)` = `100 / zoom`. No game DLL is modified.
- `ZoomController.Reapply()` re-runs `OnResolutionChanged` so a change shows immediately.
- Zoom is a percent: 100 = native, larger = zoomed in. `ZoomFactor` is saved in the config. It is reset to 100 when the real resolution changes (`GameSettings.OnResolutionChanged`). Values are clamped to Min/MaxZoomPercent.
- Zoom steps (`ZoomMath.BuildStops`): 100, min, max, and for each height in `ZoomStopHeights` the value `round(100 * nativeHeight / refHeight)`, plus an evenly spaced fallback grid (`ZoomFallbackStepPercent`) so zoom in/out never gets stuck (for example at 2160p there is no preset below 100). `StepTolerance` (0.01) stops float noise from making a step do nothing.
- Keep `ZoomMath` and `ControllerBinding` pure (no Unity types) so the tests can link them.

## Controller input

- Read through Rewired (the game's own input library), not Unity's legacy input, so buttons have real names and analog triggers work.
- Binding text stored in config: `None`, `Button:<name>`, `Axis:<name>:+|-`, or legacy `JoystickButtonN` (old configs still work).
- Rewired has two names per element: `Element.name` (generic, like "Button 5") and `Element.elementIdentifier.name` (friendly, like "Right Shoulder"). Always show the friendly one.
- Name aliases are grouped (Right Shoulder / RB / R1, Right Trigger / RT / R2 / ZR, ...) so PlayStation, Xbox and Switch names match each other.
- Press threshold for axes is 0.6. An axis binding falls back to a same-named digital button (Switch Pro ZL/ZR).
- Files: `ControllerBinding` (pure parse/describe/match), `GamepadInput` (Rewired adapter), `GamepadTrigger` (cached per-binding poller), `GamepadButtonDrawer` (F1 click-to-bind UI).
- `Gamepad/DisableInMenus` (default on): controller hotkeys are ignored while `LazyWindowsStackController.ActiveWindow != null`. Keyboard keys always work. The check is one property read per frame.
- Defaults: Right Trigger zooms in, Left Trigger zooms out, reset is not set. Open question: commenters suggest avoiding LB/LT because of combat.
- Not supported yet: D-pad.

## Settings UIs

- **F1 (BepInEx ConfigurationManager):** optional. Hotkeys use a `KeyboardShortcut`; the controller entries are strings drawn with `GamepadButtonDrawer` through `ConfigurationManagerAttributes { CustomHotkeyDrawer = ... }` (a minimal copy of the tag class, so there is no compile-time dependency on ConfigurationManager).
- **GK2 Mod Framework Mods menu:** optional, GUID `ru.superman4eg.gk2.framework`. Handled only by the bridge plugin (`com.gravezoom.mod.framework`).
  - The bridge has a hard dependency on Grave Zoom and only a **soft** dependency on the framework. A hard dependency makes BepInEx log an Error line ("Could not load ... missing dependencies") for everyone without the framework. With a soft dependency the bridge always loads, checks `Chainloader.PluginInfos` for the framework, and turns itself off silently if it is missing.
  - `FrameworkBridgePlugin` must not mention any framework type. All framework code is in `FrameworkRegistration`, called through a `[MethodImpl(NoInlining)]` method only after the check, so the framework DLL is never loaded when it is absent. Keep it that way.
  - `Register` calls `FrameworkApi.RegisterMod(mod, mainPlugin.Config)`. The bridge registers the same Section/Key/type as the main plugin, so the framework returns the existing `ConfigEntry` and both UIs edit the same values. Defaults in the bridge must match `Plugin.cs`.
  - Only everyday settings are shown (zoom slider, keys, gamepad toggle, controller buttons, indicator). Advanced ones (min, max, fallback step, step heights) are F1 only, to keep the menu short.
  - **Disable after restart:** the bridge sets `frameworkManagesEnabledState: true`, so the framework writes `Framework/Enabled` into Grave Zoom's config. `Plugin.Awake` binds that same entry (hidden from F1 with `Browsable = false`) and skips loading when it is false.
  - **Controller buttons in the Mods menu:** the framework 0.1.12 has no clickable row, so the rows are read-only text pointing to F1. A patched framework adds `Gk2Settings.AddButton`. The bridge looks it up by reflection and, if found, uses it to rebind (`BeginCapture` / `Update` in `FrameworkBridgePlugin`, using the shared `GamepadInput.cs`). The rebind code lives in Grave Zoom, the framework only draws the row. Without `AddButton` it falls back to read-only text.
  - The bridge compiles `ControllerBinding.cs` and `GamepadInput.cs` from the main project. `GamepadInput` logs through `Plugin.Log`, so the bridge has a tiny stand-in `Plugin` class.
  - The main plugin logs an INFO line in `Start()` if the framework is not installed ("falling back to the BepInEx (F1) settings menu"). `Start` is used because the framework may load after Grave Zoom's `Awake`. This is the only message in that case.
  - **Known framework limit (0.1.12):** controller navigation on its settings page does not work. Its navigation controller has `useGridSkippedIfListEmpty` off, so focus never leaves Back; toggles and sliders are not focusable; the list does not scroll to the focused row. A patch that fixes this and adds `AddButton` and right-stick scrolling is kept outside the repo (`gk2-framework-controller-support.patch`), waiting for the framework author's approval before any pull request.
- `VersionConsistencyTests` checks that the bridge's version and main GUID match the main plugin. Bump both together.

## Build, test, package

- Build with .NET SDK. On the dev machine builds run in a distrobox named `modding` (dotnet 10 + ilspycmd) to keep the host clean: `distrobox enter modding -- bash -c 'cd <repo>; dotnet build -c Release'`.
- Projects: `GraveZoom/GraveZoom.csproj`, `GraveZoom.Framework/GraveZoom.Framework.csproj` (needs a copy of `GK2.Framework.dll`, set `FrameworkDll`), tests with `dotnet test tests/GraveZoom.Tests`, bats with `bats tests/install.bats`.
- Release builds set `DebugType none` so the PDB path (which contains the local user name) is not baked into the DLL. Before sharing a build, grep the DLLs for the user name / home path.
- Release zip (built with `python3 -m zipfile -c`, no `zip` on the host): `BepInEx/plugins/GraveZoom/GraveZoom.dll`, `GraveZoom.Framework.dll`, and `README.md`.
- Before a release, test three cases in game: no framework, the stock framework, and (if relevant) a patched framework. Check the log for errors each time.
- Deploy for testing by copying the DLLs into `BepInEx/plugins/GraveZoom/` in the game folder. Game logs: `BepInEx/LogOutput.log`.
- The game runs under Proton on Linux. BepInEx needs the launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%`.

## Releases

- Publish only when the owner says so. Do not commit, push, tag or publish without being asked.
- Tag format `vX.Y.Z`. Always use a fresh tag: the `release: published` workflow does not fire if a tag is reused.
- On publish, `.github/workflows/virustotal.yml` scans the release zips. `install.sh` is also attached to each release (the one-liner in the README uses `releases/latest/download/install.sh`).
- Nexus Mods upload is manual. Nexus quarantined a zip that contained `install.sh`, so the Nexus zip has only the DLLs and README. Nexus web pages return 403 to scripts.

## Game facts worth remembering

- Unity 6000.3, Mono. BepInEx 5.4.23.5 (win_x64) is used on Linux too, through Proton.
- The GitHub repo is `a-solanas/GraveZoom` (public).
- Game DLLs used for compiling: `Assembly-CSharp`, `LazyBearTechnology`, `Rewired_Core`, `UnityEngine*`, plus BepInEx and `0Harmony`.

## How to work with the owner

- Simple English in the README, Nexus text and notes. Do not use "--" as a dash.
- No personal information in code, binaries, docs or commit history: no home paths, user names or emails.
- Ask before anything hard to undo or public: commit, push, tag, release, publishing, deleting.
- The owner runs anything that sets a secret (for example `gh secret set`) in their own terminal.
- Prefer small, simple code and a thin main plugin. Anything that runs per frame must be cheap.
- Test in game after each behavior change and wait for the owner's result before releasing.
- Run all tests (`dotnet test` and `bats`) before saying something is ready.
