# NOT DONE YET, still implementing. 

TODO: 
- mpermissions/mapfiles seems to be missing since 1.25
- Add BEC-Support
  -	maybe direct install in serverfiles/BattlEye
	  - config von BEC/battleeye config ändern
	- ServerExeName = DayZServer_x64.exe
	  - https://github.com/TheGamingChief/BattlEye-Extended-Controls/
	  - https://github.com/TheGamingChief/BattlEye-Extended-Controls/blob/master/BecGuide.txt
	  - https://pastebin.com/yHgZLT4b  example skript
	  - Rcon Port und Port in battlEye/BEServer.cfg
- fix QueryPort 
	- cfg file steamQueryPort=%%
- Readme add -instanceId recommendation and --profile.. maybe directly use profile by default?


base code was this howto: https://www.reddit.com/r/dayz/comments/afad51/automatically_update_and_sync_your_steam_workshop/

# WindowsGSM.DayZAuto
Plugin for WindowsGSM to run a dedicated server for DayZ with automatic Modupdates enabled

# Modlist
this Plugin needs a Modlist.txt (inside WindowsGSM/servers/%ID%/serverfiles/Modlist.txt in the format of "SteamModId,@ModName", example content:

1564026768,@Community-Online-Tools

1571965849,@DisableBaseDestruction

1572541337,@InventoryPlus

1578227776,@Permissions-Framework

You can find the Steam Workshop folder name in the meta.cpp located in the mod folder of all the mods on the Steam Workshop. It is the mod ID and it is also part of the URL of the mod when browsing it on the steam workshop.

### Give Love!
[Buy me a coffee](https://ko-fi.com/raziel7893)

[Paypal](https://paypal.me/raziel7893)
