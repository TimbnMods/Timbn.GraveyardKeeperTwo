# Timbn.GraveyardKeeperTwo.VanillaTweaks

Timbn Vanilla Tweaks fixes bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. Almost every fix and tweak can be turned off on its own in the config.

## Bug Fixes

### Green Thumb perk

The perk never worked and now adds +2 farming mastery when planting. The game does not use the player's mastery on harvest, so that part of the description still can't do anything.

### Lost tech points

Tech points can bounce through a wall and land where they can't reach you. Now any tech points stuck off walkable ground are pulled to you.

### Priest perk rounding down

The game can end up with 3.4999 faith when it should be 3.5, which rounds down instead of up, so Priest adds nothing at some parishioner counts. Faith now rounds on the intended value.

### Stuck supply zombies

Supply zombies could freeze holding an item when all storage was full. They now walk back to their station and show the game's storage full icon until there is room.

### Lumberjack, miner, clay and sand zombies stuck swinging

Workstations can act weird when destroying them while a zombie is working or remvoing zombies from working. This fixes stuck zombies.

### Getting stuck on the ground

Fixes spots where you can get stuck, such as the stairs from the dock up to the stone pier on the far right of the Port Area. Each fix does nothing once the game fixes that spot.

### Lost battle rewards

Battle rewards are dropped at your feet after a win. With full bags the game knocks an item away every time you walk into it, so following it pushes it further until it lands somewhere you can't reach, and rewards like the Defender's Emblem were lost for good. Going to sleep brings every item battles give as a reward (Defender's Emblem, Victory Banner, Keys for Looters, Town Gratitude, and Zombie Goo) lying anywhere in the world back to your feet, including ones lost before you installed this.

### Items in the wrong bag used forever

The chest window let you put anything into any of your bags, like fish in the alchemy bag. Crafting counted an item in the wrong bag but never used it up, so one item could be used forever. Bags now only take what they are meant to. Anything already in the wrong bag is moved out when you load, into the chest holding the bag or your inventory, or dropped at your feet if there is no room.

### Zombies filling the well and study table

Partly fixed by the game developers, only for wells built on 1.006 or later and study tables in saves started on 1.005 or later.

When the storage nearby is full, a zombie can put its item somewhere that is meant to hold only one thing. Flax in the garden well stops it giving water, and junk in the study table makes it unusable and every science you make afterwards is lost. This makes older wells and study tables, and any other storage built before the game limited what it takes, follow that limit too. Anything already in the wrong place is dropped beside it when you load.

### Destroying a bag deletes what's inside

Destroying a bag also destroyed everything in it, even items that can't be destroyed on their own like the Inquisitor's medallion or the King's signet. Destroy is now greyed out on a bag that still has something in it. Set Bug Fixes BagDestroy to BlockIfImportant to only block it while the bag holds something that can't be destroyed, or NoBlocking for the game's behavior.

## Soft Locks

### Quest building built too early

Partly fixed by the game developers, still needed for saves where it was already built early.

Building the church choir or organ before being asked will lock the quest from completing. The game only noticed the build at the moment it happened, not if it has happened in the past. The quest now finishes when you load a save with the choir or organ already built.

### Stations you can't reach to dismantle

A station up against a fence or wall might be blocked from dismantling. This will allow you to still work on it.

### Locked out of boards

If you are soft locked and run out of boards without a sawhorse or circular saw, Larry will help you out.

### Lost story items

Losing story items before you use them can soft lock the game. Larry will have found it and gives it back.

## Vanilla Tweaks

### Unstuck hotkey

No more getting wedged into a tight spot. A configurable key (default Ctrl+U) moves you to the nearest open ground. Clear Tweaks UnstuckKey to turn the key off, and set Tweaks UnstuckRange (1 to 100, default 30) for how far it may move you.

### Move identical items from bags

The chest window's move all identical items button only took items from your main inventory. It now also takes matching items out of your bags. Turn off Tweaks MoveIdenticalFromBags in the config to go back to the game's behavior.

### Bed saving without being tired

Using the bed saves the game even if you are not tired enough to sleep. This is off by default. Turn on Tweaks BedSaveWhenRested in the config to use it.

### Higher tech point cap

The game caps red, green, and blue tech points at 999 each and throws away anything past that. This is off by default. Raise Tweaks TechPointCap in the config (up to 999999) to hold more.

## Translating

Every line this mod adds is in `lang/en.txt` inside its folder. Copy it to your game's language id, such as `lang/de.txt`, `lang/fr.txt`, or `lang/pt-br.txt`, and translate the text after each `=`. The game shows your translation when it runs in that language, and English for any line you leave out.

## Fixed By Game Updates

- Crematorium bodies getting stuck
- Fishing spots refilling slower and slower
- The study table getting stuck partway through a study or decompose
- Zombie cooks throwing away the second ingredient of oven recipes
- Buildings in the resurrection lab that could never be removed

## Requirements

- [BepInEx for Graveyard Keeper 2](https://www.nexusmods.com/graveyardkeeper2/mods/48)
- [Timbn Core](https://www.nexusmods.com/graveyardkeeper2/mods/84)

## Installation

### With Vortex

1. Install BepInEx for Graveyard Keeper 2
2. Install Timbn Core
3. Download with "Mod Manager Download" from the Files tab or the Vortex download button

### Without Vortex

1. Install BepInEx for Graveyard Keeper 2, start the game once, and quit at the main menu. This creates the `BepInEx/plugins` and `BepInEx/config` folders.
2. Install Timbn Core
3. Download the Timbn.GraveyardKeeperTwo.VanillaTweaks zip file.
4. Extract the zip into `BepInEx/plugins`. You should end up with `Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.Core/` and `Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.VanillaTweaks/`.

### Check it works

The bottom right of the main menu lists Timbn Core and Timbn Vanilla Tweaks along with any other Timbn mods.

## Troubleshooting

If a game update changes something a Timbn mod relies on, that mod stays off instead of running half broken, and `BepInEx/LogOutput.log` says which part failed. If Core itself fails, every Timbn mod stays off. The startup line and a popup on the main menu also warn when the game version differs from the one these mods were tested on. Include that line when reporting a problem.

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.VanillaTweaks.cfg`

After the first launch, every fix and tweak can be turned on or off in the config file. Restart the game after changing it.

## Uninstalling

Delete the `Timbn.GraveyardKeeperTwo.VanillaTweaks` folder from `BepInEx/plugins`.

## License

Mozilla Public License 2.0, see `LICENSE.txt`.
