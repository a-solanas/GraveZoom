# Grave Zoom - Graveyard Keeper 2

[![Latest release](https://img.shields.io/github/v/release/a-solanas/GraveZoom)](https://github.com/a-solanas/GraveZoom/releases/latest)
[![VirusTotal scan](https://github.com/a-solanas/GraveZoom/actions/workflows/virustotal.yml/badge.svg)](https://github.com/a-solanas/GraveZoom/actions/workflows/virustotal.yml)

The game's "Screen Size" setting locks your zoom to your resolution - only a couple of fixed steps, nothing in between. Grave Zoom lets you set the zoom to any value, at any resolution.

## Hotkeys (default)

| Action | Keyboard | Controller |
| --- | --- | --- |
| Zoom in | Page Up | Right trigger |
| Zoom out | Page Down | Left trigger |
| Reset zoom | Home | not set |

Controllers are read through the game's own input system, so buttons show up with your controller's own names (for example "Square" on PlayStation, "B" on Xbox). Any button or trigger can be rebound in the F1 menu. The D-pad isn't supported yet.

## Install

1. Get [BepInEx](https://github.com/BepInEx/BepInEx/releases) (Mono, x64) for the game, if you don't have it yet.
2. Unzip the [latest Grave Zoom release](https://github.com/a-solanas/GraveZoom/releases/latest) into your Graveyard Keeper 2 folder. You should end up with:
   - `BepInEx/plugins/GraveZoom/GraveZoom.dll`
   - `BepInEx/plugins/GraveZoom/GraveZoom.Framework.dll` (only used if you also have GK2 Mod Framework)

### Linux / Steam Deck

`install.sh` does all of the above for you - it finds your game, installs BepInEx if needed, and installs Grave Zoom:

```
curl -fsSL https://github.com/a-solanas/GraveZoom/releases/latest/download/install.sh | bash
```

On Steam Deck that's the whole setup, launch option included. On other Linux distros, add this to the game's Steam launch options yourself so BepInEx can load:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

## Settings

Change any setting in-game, no file editing needed:

- **F1 menu** ([BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)) - every setting, plus rebinding hotkeys and controller buttons.
- **Mods menu** ([GK2 Mod Framework](https://www.nexusmods.com/graveyardkeeper2/mods/42), optional) - the everyday settings, from the main menu or pause menu. Neither mod needs the other. Controller navigation there is limited in the current framework version (0.1.12); it works with mouse and keyboard.

<details>
<summary>All F1 settings</summary>

| Setting | What it does |
| --- | --- |
| ZoomFactor | Current zoom, as a percent. 100 = no change. Type any number; zooming in/out from it jumps to the nearest preset. |
| RememberZoom | On by default. Keeps your last zoom after the screen resolution changes. |
| MinZoomPercent / MaxZoomPercent | The allowed zoom range. |
| ZoomStopHeights | Resolution heights used to build the preset zoom values. |
| ZoomIn / ZoomOut / ResetZoom | The three hotkeys. |
| ZoomInButton / ZoomOutButton / ResetZoomButton | The controller buttons for the same actions. Click the setting, then press a button or pull a trigger to bind it (Clear turns it off). |
| DisableInMenus (Gamepad) | On by default. Controller buttons do nothing while a game menu is open, so they don't clash with menu controls. |
| ShowZoomIndicator | Shows the zoom value on screen after each change. |

</details>

## Building from source

Needs the .NET SDK and the game with BepInEx installed; the build reads game DLLs from your game folder.

By default it looks in `$(HOME)/.local/share/Steam/steamapps/common/Graveyard Keeper 2`. On another path or on Windows, add a git-ignored `Directory.Build.local.props` in the repo root:

```xml
<Project>
  <PropertyGroup>
    <GamePath>C:\path\to\Graveyard Keeper 2</GamePath>
  </PropertyGroup>
</Project>
```

```
dotnet build GraveZoom/GraveZoom.csproj -c Release
```

The DLL lands in `GraveZoom/bin/Release/GraveZoom.dll`.

The GK2 Mod Framework integration is a separate, optional project that needs a copy of `GK2.Framework.dll` (set `FrameworkDll` in `Directory.Build.local.props` if it's not in `BepInEx/plugins/` already):

```
dotnet build GraveZoom.Framework/GraveZoom.Framework.csproj -c Release
```

Tests: `dotnet test tests/GraveZoom.Tests` (C# logic) and `bats tests/install.bats` (install script).
