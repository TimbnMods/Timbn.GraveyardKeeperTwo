Timbn Core

Timbn Core is the shared framework every Timbn mod for Graveyard Keeper 2 is built on. It does nothing on its own. Install it when a Timbn mod lists it as a requirement.


REQUIREMENTS

BepInEx 5 (x64, Mono)


INSTALLATION

1. Install BepInEx 5 (skip this if you already have it)
   a. Download BepInEx 5.4.23.5 for Windows x64 (BepInEx_win_x64_5.4.23.5.zip) from https://github.com/BepInEx/BepInEx/releases
      Use BepInEx 5, not 6.
   b. Open the game folder. In Steam, right click Graveyard Keeper 2, then Manage > Browse local files.
   c. Extract the zip into that folder, so BepInEx, winhttp.dll, and doorstop_config.ini sit next to GraveyardKeeper2.exe.
   d. Start the game once and quit at the main menu. This creates the BepInEx/plugins and BepInEx/config folders.

2. Install Timbn Core
   Extract this zip into Graveyard Keeper 2/BepInEx/plugins/
   You should end up with:
   Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.Core/

3. Check it works
   Start the game, then open BepInEx/LogOutput.log. You should see "Core 1.0.0 started".


SETTINGS

BepInEx/config/Timbn.GraveyardKeeperTwo.Core.cfg has one setting, Enabled. Turning it off turns off every Timbn mod at once. Restart the game after changing it.


TROUBLESHOOTING

If a game update changes something a Timbn mod relies on, that mod stays off instead of running half broken, and BepInEx/LogOutput.log says which part failed. If Core itself fails, every Timbn mod stays off. The startup line also warns when the game version differs from the one these mods were tested on. Include that line when reporting a problem.


UNINSTALLING

Delete the Timbn.GraveyardKeeperTwo.Core folder from BepInEx/plugins. Every Timbn mod needs Core, so remove those too.


FOR MODDERS

Timbn.GraveyardKeeperTwo.Core.xml next to the DLL holds the API documentation for your IDE.


LICENSE

Mozilla Public License 2.0, see LICENSE.txt.
