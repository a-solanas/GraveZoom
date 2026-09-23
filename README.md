# Grave Zoom - Graveyard Keeper 2

The game's "Screen Size" setting locks your zoom level to your resolution. For example, at 2560x1440 you can only pick "x2" or "x3" - nothing in between, and no other value. Grave Zoom removes that limit. You keep your resolution, but you can set the zoom to any value you want.

## Hotkeys (default)

| Key | Action |
| --- | --- |
| Page Up | Zoom in |
| Page Down | Zoom out |
| Home | Reset zoom |

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
