# Timbn.GraveyardKeeperTwo.VanillaTweaks

Timbn Vanilla Tweaks fixes bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. Almost every fix and tweak can be turned off on its own in the config.

## Bug Fixes

### Green Thumb, Master Brewer and Sommelier perks

These perks never worked. Green Thumb now adds +2 farming mastery when planting, Master Brewer +2 when brewing beer and mead, and Sommelier +3 when making wine, so you can brew and make the better wines sooner. The game does not use the player's mastery on harvest, so that part of Green Thumb's description still can't do anything.

### Lost tech points

Tech points can bounce through a wall and land where they can't reach you. Now any tech points stuck off walkable ground are pulled to you.

### Priest perk rounding down

The game can end up with 3.4999 faith when it should be 3.5, which rounds down instead of up, so Priest adds nothing at some parishioner counts. Faith now rounds on the intended value.

### Stuck supply zombies

Supply zombies could freeze holding an item when all storage was full. They now walk back to their station and show the game's storage full icon until there is room.

### Lumberjack, miner, clay and sand zombies stuck swinging

Workstations can act weird when destroying them while a zombie is working or removing zombies from working. This fixes stuck zombies.

### Lost battle rewards

Battle rewards are dropped at your feet after a win. With full bags the game knocks an item away every time you walk into it, so following it pushes it further until it lands somewhere you can't reach, and rewards like the Defender's Emblem were lost for good. Going to sleep brings every item battles give as a reward (Defender's Emblem, Victory Banner, Keys for Looters, Town Gratitude, and Zombie Goo) lying anywhere in the world back to your feet, including ones lost before you installed this.

### Bait in bags

Fishing ignored bait kept in a bag, so with all of it in a fishing bag the Keeper said there was no bait and never cast. Bait in any of your bags now counts, and it is used from the bag like the game already does once fishing starts.

### Crash with conveyor loops

A loop of conveyors that feeds into another loop, such as a splitter sending part of its coal back into the chest before it, made the game close on its own a few seconds after loading once the first loop ran empty or jammed. It kept happening every time that save loaded. The game no longer gets stuck on those loops, and your conveyors work as before. A notice appears in the corner when such a loop is found. Yellow arrows trace the loop and a red square marks the belt to remove to break it. The arrows and square can be turned off in the config.

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

### Items sort into their bags

The game only put an item into a bag that already held some of it, and otherwise filled your main inventory first. Now anything you pick up, craft, buy, or take from a chest goes straight into the bag made for it, such as fish and bait into a fishing bag or seeds into a farming bag, while that bag has room. Universal bags work as before, since they take almost anything. Moving an item out of a bag yourself leaves it where you put it. Turn off Tweaks SortIntoBags in the config to go back to the game's behavior.

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
- Zombies filling the well and study table
- Destroying a bag deleting everything inside it
- Items in the wrong bag being used forever
- Getting stuck on the stairs from the dock up to the stone pier in the Port Area

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

After the first launch, every fix and tweak can be turned on or off in the config file. A config manager applies changes straight away, even in the middle of a game. Only General Enabled needs a restart, and so does editing the file by hand.

## Uninstalling

Delete the `Timbn.GraveyardKeeperTwo.VanillaTweaks` folder from `BepInEx/plugins`.

## License

Mozilla Public License 2.0, see `LICENSE.txt`.
