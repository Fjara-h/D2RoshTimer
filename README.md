# D2RoshTimer
[1. This is version for normal use. There are no error/issue message popups.](https://github.com/robuhde/D2RoshTimer/raw/master/Downloads/D2RoshTimer%20%20-%20ErrorSupressed.zip)

[2. This is a version I recommend using to test why something is not working and/or to learn how/when to use it.](https://github.com/robuhde/D2RoshTimer/raw/master/Downloads/D2RoshTimer%20-%20ContainsErrorPopups.zip)

I recommend trying first with 2. If you understand when to use and how to use it, download 1. I will add an option soon to the main window to enable/disable error popups so that there is only 1 download.

Run the .exe, a window will pop up where you can set your keybind. Press Save whenever you are ready. After loading into a game and you can see the in-game clock (before or after the horn), press the hotkey when Rosh Falls/Hero Picks up Aegis Kill time, aegis reclaim time, rosh early spawn, rosh late spawn will be set on your clipboard. Then open chat and paste (ctrl + v). 

Unfortunately if players do not pick up aegis shortly after rosh dies, then the reclaim timer will be off. GSI for non-spectator/observer games (you are the player), there is no ability to aquire information about when other heroes pick up aegis.
I will be adding in support to edit aegis reclaim time if you are the hero that picks it up.

Only 1 instance can run at once. 
If you want to delete the files this program creates, the program settings are here:
C:\Users\<User Account>\AppData\Local\Fjara\D2RoshTimer - Delete D2Rosh Timer folder
The Gatestate Integration Files are Located within your dota 2 cfg folder:
~\dota 2 beta\game\dota\cfg\gamestate_integration

### Current MesssageBox Error Pop-Ups:
Another instance already running - D2RoshTimer.exe is already running, if not visible in the taskbar/notification area, open Task Manager and manually end task.

Registry key for Dota 2 not found, cannot create Gamestate Integration file - 32-bit System or dota and/or steam are not installed.

GameStateListener could not start. Try running as Administrator - Missing/Incorrect .cfg file, the server/coordinator are offline, or program needs to be run as administrator.

This only runs when loaded into a game - Hotkey pressed outside correct gamestate. Can only activate when in a game. (Pre-Game or Game in Progress)

GSI failed to update currentTime - Missing/Incorrect .cfg file or server/coordinator are offline.

Dota 2 not running - dota2.exe not running or detected.

You are running again to soon - There is an in-built pause that stops it from running for 4 seconds between runs.

### Issues
If you run into an issue please let me know by posting an issue here or message me on twitter @fjara_ or on reddit /u/Fjarah
Please try to be thorough in explaining what happened, how, if you are able to reproduce it and your system specs. There is a possibility slower systems may have an issue with handling threads slowly. 

## Requirements:
64-Bit Windows (Tested on Win10 and Win7)

.Net Framework 4.5.2 or higher

Dota 2 & Steam Installed

## Unusable Keys for main key:
### Reserved:
Left and Right Control

Left and Right Alt

Left and Right Windows Key

Left and Right Shift

### Disabled:
Print Screen, Insert, PageUp, PageDown, End, Delete, Backspace, Up Arrow, Down Arrow, Left Arrow, Right Arrow
F10 may not be usable and depends on the system

Non-activated Numpad keys

## External Libraries Used:
Dota2GSI by Antonpup - Updated by [Me](https://github.com/robuhde/Dota2GSI)

Newtonsoft.Json

Hardcodet.NotifyIcon.Wpf by Philipp Sumi

NHotkey.Wpf by Thomas Levesque

Fody & Costura.Fody

