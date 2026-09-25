# Grave Zoom - Graveyard Keeper 2

The game's "Screen Size" setting locks your zoom level to your resolution. For example, at 2560x1440 you can only pick "x2" or "x3" - nothing in between, and no other value. Grave Zoom removes that limit. You keep your resolution, but you can set the zoom to any value you want.

## Hotkeys (default)

| Action | Keyboard | Controller |
| --- | --- | --- |
| Zoom in | Page Up | Right trigger (R2) |
| Zoom out | Page Down | Left trigger (L2) |
| Reset zoom | Home | not set |

You can use buttons and triggers (L2/R2, LT/RT, ZL/ZR). Controllers are read through the game's own input system, so buttons are found by name and shown with the names on your controller (for example "Square" on a PlayStation controller, or "B" on an Xbox controller). By default the right trigger zooms in and the left trigger zooms out (R2 and L2 on a PlayStation controller, ZR and ZL on a Switch Pro controller). You can change any of them in the F1 menu (see below). The D-pad is not supported yet.

## Requirements

- [BepInEx](https://github.com/BepInEx/BepInEx/releases) (Mono, x64). Tested with version 5.4.23.5, but any up-to-date BepInEx 5 (Mono) build should work.

### Optional: BepInEx Configuration Manager

[BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) adds a settings menu you can open in-game by pressing F1. From there you can change any setting below, or rebind the hotkeys, without editing any file.

| Setting | What it does |
| --- | --- |
| ZoomFactor | The current zoom, as a percent. 100 means "no change". You can type in any number you want; zooming in/out from it then jumps to the closest preset value. |
| MinZoomPercent | The lowest zoom percent allowed. |
| MaxZoomPercent | The highest zoom percent allowed. |
| ZoomStopHeights | The list of resolution heights used to build the preset zoom values. |
| ZoomIn / ZoomOut / ResetZoom | The three hotkeys. You can change them here. |
| ZoomInButton / ZoomOutButton / ResetZoomButton | The controller buttons for the same three actions. The right trigger zooms in and the left trigger zooms out by default, reset is not set. Click the setting, then press a button or pull a trigger on your controller to bind it (Clear turns it off). If you upgraded from an older version, your old buttons still work. Click the setting and press the button again to get the new names. |
| DisableInMenus (Gamepad) | On by default. The controller buttons do nothing while a game menu is open (for example crafting), so they do not clash with menu controls. The keyboard keys still work. |
| ShowZoomIndicator | Turns the on-screen zoom readout on or off. |

## Install

Unzip this into your Graveyard Keeper 2 folder. You should end up with this file:
`BepInEx/plugins/GraveZoom/GraveZoom.dll`

## Linux / Steam Deck

Run [`install.sh`](install.sh) instead of installing by hand. It finds your Graveyard Keeper 2 install (across any Steam library, including an SD card), installs BepInEx and Configuration Manager if you don't have them yet, and downloads and installs the latest Grave Zoom release.

```
curl -fsSL https://github.com/a-solanas/GraveZoom/releases/latest/download/install.sh | bash
```

(If you'd rather not pipe a script straight into `bash`, download it first and read it, then run `bash install.sh`.)

- **On Steam Deck**, that's it - the script also sets the required Steam launch option for you automatically, since its Steam config is always in the same place.
- **On other Linux distros**, everything else is automatic, but you still need to set the launch option yourself, since Steam library setups vary too much to do this safely everywhere. Add this to the game's Steam launch options so BepInEx can load:

```
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

## Building from source

You need the .NET SDK and Graveyard Keeper 2 with BepInEx already installed. The build reads game DLLs from your game folder.

By default, the build looks in `$(HOME)/.local/share/Steam/steamapps/common/Graveyard Keeper 2` (Linux Steam path). If your game is elsewhere, or you use Windows, create a file named `Directory.Build.local.props` in the repo root (it is git-ignored):

```
<Project>
  <PropertyGroup>
    <GamePath>C:\path\to\Graveyard Keeper 2</GamePath>
  </PropertyGroup>
</Project>
```

Replace `C:\path\to\Graveyard Keeper 2` with your actual game folder path.

Build with:
```
dotnet build GraveZoom/GraveZoom.csproj -c Release
```

The DLL is in `GraveZoom/bin/Release/GraveZoom.dll`. Copy it to `BepInEx/plugins/GraveZoom/` in your game folder.

To run tests:
- `bats tests/install.bats` - tests the install script (needs bats)
- `dotnet test tests/GraveZoom.Tests` - tests the C# logic
