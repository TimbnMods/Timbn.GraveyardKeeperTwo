Timbn Vanilla Tweaks

Timbn Vanilla Tweaks fixes bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. Almost every fix and tweak can be turned off on its own in the config.

BUG FIXES

Study Table eating your science
If all storage in the tower is full and the study table has no science in it, a zombie can put its item in the table. The table is then unusable and every science you make afterwards is lost. This puts a science only filter on the table so that can't happen, and anything already stuck in the table is moved out when you load.

Study table stopping partway
A broken popup on the workstation can stop the study table partway through. Pressing Study could take your faith and science and start nothing, and pressing Decompose could leave the table stuck. Both now are fixed and a table already stuck is freed when you load.

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

Chests in the resurrection lab can't be removed
Anything you built in the resurrection lab from the morgue build desk could never be removed but this fixes it.

Fishing spots refilling slower and slower
A fishing spot will refill slower and slower as you progress through more days. This will fix it to refill at the normal rate.

SOFT LOCKS

Quest building built too early

Building the church choir before Agatha asks for it will lock the quest from completing. The game only noticed the build at the moment it happened, not if it has happened in the past. The quest now finishes when you load a save with the choir or organ already built.

Stations you can't reach to dismantle

A station up against a fence or wall might be blocked from dismantling. This will allow you to still work on it.

Locked out of boards

If you are soft locked and run out of boards without a sawhorse or circular saw, Larry will help you out.

VANILLA TWEAKS

Unstuck hotkey
No more getting wedged into a tight spot. A configurable key (default Ctrl+U) moves you to the nearest open ground.

Bed saving without being tired
Using the bed saves the game even if you are not tired enough to sleep. This is off by default. Turn on Bed SaveWhenRested in the config to use it.

Higher tech point cap
The game caps red, green, and blue tech points at 999 each and throws away anything past that. This is off by default. Raise TechPoints Cap in the config (up to 999999) to hold more.

REQUIREMENTS

BepInEx for Graveyard Keeper 2 (https://www.nexusmods.com/graveyardkeeper2/mods/48)
Timbn Core (https://www.nexusmods.com/graveyardkeeper2/mods/84)

INSTALLATION

With Vortex

1. Install BepInEx for Graveyard Keeper 2
2. Install Timbn Core
3. Download With "Mod Manager Download" from the files tab or Vortex download button.

Without Vortex

1. Install BepInEx for Graveyard Keeper 2 and start the game once and quit at the main menu. This creates the BepInEx/plugins and BepInEx/config folders.
2. Install Timbn Core
3. Install Vanilla Tweaks, download the Timbn.GraveyardKeeperTwo.VanillaTweaks zip file.
4. Extract this zip into the BepInEx plugin folder. You should end up with:
  /Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.Core/
  /Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.VanillaTweaks/

Check It Works

The bottom right of the main menu will list Timbn Core and Timbn Vanilla Tweaks along with any other Timbn mods.

TROUBLESHOOTING

If a game update changes something a Timbn mod relies on, that mod stays off instead of running half broken, and BepInEx/LogOutput.log says which part failed. If Core itself fails, every Timbn mod stays off. The startup line and a popup on the main menu also warn when the game version differs from the one these mods were tested on. Include that line when reporting a problem.

SETTINGS

After the first launch, every fix and tweak can be turned on or off in the config file. Restart the game after changing it.
  /BepInEx/config/Timbn.GraveyardKeeperTwo.VanillaTweaks.cfg

UNINSTALLING

Delete the Timbn.GraveyardKeeperTwo.VanillaTweaks folder from BepInEx/plugins.

LICENSE

Mozilla Public License 2.0, see LICENSE.txt.
