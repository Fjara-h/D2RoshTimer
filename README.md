# D2RoshTimer
Press a hotkey, get the in-game time as rosh's death, calculate aegis reclaim, early spawn, late spawn timings and place it in clipboard for you to paste.

[Download](https://github.com/robuhde/D2RoshTimer/raw/master/Downloads/D2RoshTimer.exe)

![Example](https://raw.githubusercontent.com/robuhde/D2RoshTimer/master/Resources/Sample.png)

This uses valve's gamestate integration which sends data when requested. It is used in many other programs and will never result in a VAC ban, there is absolutely no chance of this. The only thing people may think is that it is somehow unfair, but it is using information available to anyone who wants it and is actively upkept by valve for use.

Run the .exe, a window will pop up where you can set your keybind. Press Save whenever you are ready. After loading into a game and you can see the in-game clock (before or after the horn), press the hotkey when Rosh Falls, the current in game time will be recorded and rosh kill time, aegis reclaim time, rosh early spawn, rosh late spawn will be set on your clipboard. Then open chat and paste (ctrl + v). Only 1 instance can run at any point.

The timer is based on the clock in-game when the hotkey is pressed, aegis reclaim is just +5 minutes to kill time. There is no way to get client/player side to get rosh's status or aegis's status, only in spectator mode. 

If for some reason the gamestate_integration_roshtimer.cfg  cannot be created in the dota 2 beta folder. [Download this and copy it to where your "dota 2 beta" folder is.](https://github.com/robuhde/D2RoshTimer/raw/master/Downloads/Manual%20cfg%20placement.zip)

## Files Created

If you want to delete the files this program creates, the program settings are here:

C:\Users\<User Account>\AppData\Local\Fjara\D2RoshTimer.exe~~~~ - Delete D2Rosh Timer folder

The Gatestate Integration Files are Located within your dota 2 cfg folder:

~\dota 2 beta\game\dota\cfg\gamestate_integration


### Current MesssageBox Error Pop-Ups:

- Another instance already running - D2RoshTimer.exe is already running, if not visible in the taskbar/notification area, open Task Manager and manually end task.

- Registry key for Dota 2 not found, cannot create Gamestate Integration file - 32-bit System or dota and/or steam are not installed.

- GameStateListener could not start. Try running as Administrator - Missing/Incorrect .cfg file, the server/coordinator are offline, or program needs to be run as administrator.

- This only runs when loaded into a game - Hotkey pressed outside correct gamestate. Can only activate when in a game. (Pre-Game or Game in Progress)

- GSI failed to update currentTime - Missing/Incorrect .cfg file or server/coordinator are offline.

- Dota 2 not running - dota2.exe not running or detected.

- You are running again to soon - There is an in-built pause that stops it from running for 4 seconds between runs.

### Issues

D2RoshTimer should only close when you right click on it in the notification taskbar and select "Exit". If it does close prior to that, then there is some issue and I would recommend turning on error messages if not already. Also, If you run into an issue such as this please let me know by posting an issue here or message me on twitter @fjara_ or on reddit /u/Fjarah

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
