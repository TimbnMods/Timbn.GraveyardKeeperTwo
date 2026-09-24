Timbn Core

Timbn Core is the shared framework every Timbn mod for Graveyard Keeper 2 is built on. It does nothing on its own. Install it when a Timbn mod lists it as a requirement.

REQUIREMENTS

BepInEx for Graveyard Keeper 2 (https://www.nexusmods.com/graveyardkeeper2/mods/48)

INSTALLATION

With Vortex

1. Install BepInEx for Graveyard Keeper 2
2. Download With "Mod Manager Download" from the files tab or Vortex download button.

Without Vortex

1. Install BepInEx for Graveyard Keeper 2 and start the game once and quit at the main menu. This creates the BepInEx/plugins and BepInEx/config folders.
2. Install Timbn Core, download the Timbn.GraveyardKeeperTwo.Core zip file.
3. Extract this zip into the BepInEx plugin folder. You should end up with:
  /Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.Core/

Check It Works

The bottom right of the main menu will list Timbn Core along with any other Timbn mods.

SETTINGS

There is one setting, Enabled. Turning it off turns off every Timbn mod at once. Restart the game after changing it.
  /BepInEx/config/Timbn.GraveyardKeeperTwo.Core.cfg

TROUBLESHOOTING

If a game update changes something a Timbn mod relies on, that mod stays off instead of running half broken, and BepInEx/LogOutput.log says which part failed. If Core itself fails, every Timbn mod stays off. The startup line and a popup on the main menu also warn when the game version differs from the one these mods were tested on. Include that line when reporting a problem.

UNINSTALLING

Delete the Timbn.GraveyardKeeperTwo.Core folder from BepInEx/plugins. Every Timbn mod needs Core, so remove those too.

FOR MODDERS

Timbn.GraveyardKeeperTwo.Core.xml next to the DLL holds the API documentation for your IDE.

LICENSE

Mozilla Public License 2.0, see LICENSE.txt.
