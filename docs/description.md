[color=#f1c232][size=5][b]Description[/b][/size][/color]

The game's "Screen Size" setting locks your zoom level to your resolution. For example, at 2560x1440 you can only pick "x2" or "x3" - nothing in between, and no other value. Grave Zoom removes that limit. You keep your resolution, but you can set the zoom to any value you want.

[color=#f1c232][size=5][b]Main features[/b][/size][/color]
- Zoom in and out freely with two hotkeys, independent of your screen resolution
- Works with keyboard and controller (buttons and analog triggers)
- One-tap reset back to the game's native zoom
- On-screen indicator that briefly shows the current zoom value after each change
[img]https://staticdelivery.nexusmods.com/mods/10208/images/55/55-1790110380-1845480996.png[/img]
- Optional in-game settings menu (via BepInEx Configuration Manager) to change any setting or rebind keys and controller buttons, without editing any file
- Optional support for GK2 Mod Framework: Grave Zoom shows up in its Mods menu and can be turned off there

[color=#f1c232][size=5][b]Hotkeys (default)[/b][/size][/color]
[color=#ffffff][size=3][b]Now supports controllers[/b][/size][/color]
- Page Up: Zoom in. Controller: right trigger (RT / R2 / ZR)
- Page Down: Zoom out. Controller: left trigger (LT / L2 / ZL)
- Home: Reset zoom. Controller: not set

Controller buttons are shown with the names of your controller (for example "Square" on PlayStation, "B" on Xbox). You can change any of them in the F1 menu (see below). The D-pad is not supported yet.

[color=#f1c232][size=5][b]Installation instructions[/b][/size][/color]
Extract into your Graveyard Keeper 2 folder. You should end up with these files:
BepInEx/plugins/GraveZoom/GraveZoom.dll
BepInEx/plugins/GraveZoom/GraveZoom.Framework.dll (only used if you have GK2 Mod Framework)

[color=#f1c232][size=5][b]Requirements[/b][/size][/color]
BepInEx (Mono, x64). Tested with version 5.4.23.5, but any up-to-date BepInEx 5 (Mono) build should work.
https://github.com/BepInEx/BepInEx/releases

[color=#f1c232][size=5][b]Optional: BepInEx Configuration Manager[/b][/size][/color]
https://github.com/BepInEx/BepInEx.ConfigurationManager
Adds a settings menu you can open in game by pressing F1. From there you can change any setting below, or rebind the hotkeys, without editing any file.

Settings available there:
- ZoomFactor: the current zoom, as a percent. 100 means "no change". You can type in any number you want. Zooming in/out from it then jumps to the closest preset value.
- MinZoomPercent: the lowest zoom percent allowed.
- MaxZoomPercent: the highest zoom percent allowed.
- ZoomStopHeights: the list of resolution heights used to build the preset zoom values.
- ZoomFallbackStepPercent: adds extra evenly spaced zoom values, so you can always zoom in or out. 0 turns this off.
- ZoomIn / ZoomOut / ResetZoom: the three hotkeys. You can change them here.
- ZoomInButton / ZoomOutButton / ResetZoomButton: the controller buttons for the same actions. Click the setting, then press a button or pull a trigger on your controller to bind it (Clear turns it off).
- DisableInMenus (Gamepad): on by default. The controller buttons do nothing while a game menu is open (for example crafting), so they do not clash with menu controls. The keyboard keys still work. Turn it off if you want the controller zoom to work in menus too.
- ShowZoomIndicator: turns the on-screen zoom readout on or off.

[img]https://staticdelivery.nexusmods.com/mods/10208/images/55/55-1790174180-532281385.png[/img]

[color=#f1c232][size=5][b]Optional: GK2 Mod Framework[/b][/size][/color]
https://www.nexusmods.com/graveyardkeeper2/mods/42
If you use GK2 Mod Framework, Grave Zoom also shows up in its Mods menu (main menu and pause menu). There you can change the current zoom, the keyboard keys, "ignore controller in menus" and the on-screen readout, and you can turn Grave Zoom off for the next start with "Disable after restart". The controller buttons are shown there too, but with the current framework version they can only be changed in the F1 menu. The advanced settings (zoom limits and zoom steps) are also only in the F1 menu.
Controller navigation inside the framework's settings page is limited in the current framework version (0.1.12). It works with mouse and keyboard.
You do not need the framework. Without it, Grave Zoom works exactly the same and the F1 menu still works. The BepInEx log then has one info line saying Grave Zoom is using the F1 menu. That is normal.

[color=#f1c232][size=5][b]Linux (Steam Proton) users[/b][/size][/color]
Add this to the game's Steam launch options so BepInEx can load:
[code]WINEDLLOVERRIDES="winhttp=n,b" %command%[/code]
On Linux and Steam Deck you can also use the install script, which finds your game, installs BepInEx and Configuration Manager if needed, and installs Grave Zoom (on Steam Deck it also sets the launch option for you):
[code]curl -fsSL https://github.com/a-solanas/GraveZoom/releases/latest/download/install.sh | bash[/code]
Source code: https://github.com/a-solanas/GraveZoom
