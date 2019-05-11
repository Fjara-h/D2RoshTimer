# D2RoshTimer
There are 2 different versions, 1 can help you figure out what the issue is if you are having one, the other is a message-supressed version so that if something does not work, a messagebox will not pop up in interrupt you while playing.

Run the .exe, a window will pop up where you can set your keybind. Press Save whenever you are ready. After loading into a game and you can see the in-game clock, press the hotkey when Rosh Falls/Hero Picks up Aegis Kill time, aegis reclaim time, rosh early spawn, rosh late spawn will be set on your clipboard. Then open chat and paste (ctrl + v). 

Unfortunately if players do not pick up aegis shortly after rosh dies, then the reclaim timer will be off. GSI for non-spectator/observer games (you are the player), there is no ability to aquire information about when other heroes pick up aegis.
I will think about adding in support to edit aegis reclaim time if you are the hero that picks it up.

Only 1 instance can run at once. 
If you want to delete the files this program creates, the program settings are here:
C:\Users\<User Account>\AppData\Local\Fjara\D2RoshTimer - Delete D2Rosh Timer folder
The Gatestate Integration Files are Located within your dota 2 cfg folder:
~\dota 2 beta\game\dota\cfg\gamestate_integration

Current MesssageBox interrupts:
Another instance already running - You already have D2RoshTimer.exe running, if it isn't showing in the taskbar/notification area, open Task Manager and manually kill it.
Registry key not found, cannot create GSI config - System is 32-bit or dota and/or steam are not installed
GSL couldn't start, try as administrator - Missing or incorrect .cfg file or the server/coordinator are offline or port requires elevation
GSI failed to update currentTime - Missing or incorrect .cfg file or the server/coordinator are offline
Dota 2 not running or running again to soon - dota2.exe not running or detected or you are running too quickly in succession, there is an in-built pause that stops it from running for 5 seconds between runs. On slower computers there is an issue where HttpListener closes before being disposed. If you are running into this please let me know your system specs (mainly cpu I believe)

If you run into an issue please let me know by posting an issue here or message me on twitter @fjara_ or on reddit /u/Fjarah
Please try to be thorough in explaining what happened, how, and if you are able to reproduce it. I'll do my best to update with fixes and such based on that.

Requirements:
64-Bit Windows (Tested on Win10 and Win7)
.Net Framework 4.5.2 or higher
Dota 2 & Steam Installed

Unusable Keys for main key:
Reserved:
Left and Right Control
Left and Right Alt
Left and Right Windows Key
Left and Right Shift
Disabled:
Print Screen, Insert, PageUp, PageDown, End, Delete, Backspace, Up Arrow, Down Arrow, Left Arrow, Right Arrow
F10 may not be usable and depends on the system
Non-activated Numpad keys

External Libraries Used:
Dota2GSI by Antonpup
Newtonsoft.Json
Hardcodet.NotifyIcon.Wpf by Philipp Sumi
NHotkey.Wpf by Thomas Levesque
Fody & Costura.Fody

if (gs.Items.InventoryContains("item_aegis"))