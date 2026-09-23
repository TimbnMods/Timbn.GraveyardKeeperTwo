Timbn Vanilla Tweaks

Timbn Vanilla Tweaks fixes bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. Almost every fix and tweak can be turned off on its own in the config.


BUG FIXES

Study Table eating your science
If all storage in the tower is full and the study table has no science in it, a zombie can put its item in the table. The table is then unusable and every science you make afterwards is lost. This puts a science only filter on the table so that can't happen, and anything already stuck in the table is moved out when you load.

Study button taking costs and doing nothing
Sometimes pressing Study takes your faith and science and nothing starts. A broken popup on the workstation was stopping it partway through. The study now goes through.

Green Thumb perk
The perk never worked and now adds +2 farming mastery when planting. The game does not use the player's mastery on harvest, so that part of the description still can't do anything.

Crematorium bodies no longer get stuck
Putting a body in the crematorium with 0 Anatomy mastery made the crematorium unusable. It now refuses the body until you have Anatomy 1, and any body already stuck is burned when you load the game.

Lost tech points
Tech points can bounce through a wall and land where they can't reach you. Now any tech points stuck off walkable ground are pulled to you.

Zombie cooks losing ingredients
Ovens only had room for one ingredient, so zombies threw away the second ingredient of any two ingredient recipe and tried to deliver again. Ovens now have room for every ingredient, and a delivery with no room is refused instead of destroyed.

Priest perk rounding down
The game can end up with 3.4999 faith when it should be 3.5, which rounds down instead of up, so Priest adds nothing at some parishioner counts. Faith now rounds on the intended value.

Stuck supply zombies
Supply zombies could freeze holding an item when all storage was full. They now walk back to their station and show the game's storage full icon until there is room.


VANILLA TWEAKS

Unstuck hotkey
No more getting wedged into a tight spot. A configurable key (default Ctrl+U) moves you to the nearest open ground.

Bed saving without being tired
Using the bed saves the game even if you are not tired enough to sleep.

Higher tech point cap
Raises the red, green, and blue tech point cap from 999 to 9999. Set it to 999 in the config to keep the game's cap.


REQUIREMENTS

BepInEx 5 (x64, Mono)
Timbn Core


INSTALLATION

1. Install BepInEx 5 (skip this if you already have it)
   a. Download BepInEx 5.4.23.5 for Windows x64 (BepInEx_win_x64_5.4.23.5.zip) from https://github.com/BepInEx/BepInEx/releases
      Use BepInEx 5, not 6.
   b. Open the game folder. In Steam, right click Graveyard Keeper 2, then Manage > Browse local files.
   c. Extract the zip into that folder, so BepInEx, winhttp.dll, and doorstop_config.ini sit next to GraveyardKeeper2.exe.
   d. Start the game once and quit at the main menu. This creates the BepInEx/plugins and BepInEx/config folders.

2. Install Timbn Core
   Extract the Timbn Core zip into Graveyard Keeper 2/BepInEx/plugins/

3. Install Vanilla Tweaks
   Extract this zip into Graveyard Keeper 2/BepInEx/plugins/
   You should end up with:
   Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.Core/
   Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.VanillaTweaks/

4. Check it works
   Start the game, then open BepInEx/LogOutput.log. You should see "Core 1.0.0 started" and "Vanilla Tweaks started".


SETTINGS

After the first launch, every fix and tweak can be turned on or off in the config file. Restart the game after changing it.
BepInEx/config/Timbn.GraveyardKeeperTwo.VanillaTweaks.cfg


UNINSTALLING

Delete the Timbn.GraveyardKeeperTwo.VanillaTweaks folder from BepInEx/plugins.


LICENSE

Mozilla Public License 2.0, see LICENSE.txt.
