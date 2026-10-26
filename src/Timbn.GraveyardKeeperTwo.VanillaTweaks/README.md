# Timbn.GraveyardKeeperTwo.VanillaTweaks

Timbn Vanilla Tweaks fixes game bugs and soft locks and adds a few small conveniences that don't cheat. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/VanillaTweaks/page.yml`, and what changed in each version is in `CHANGELOG.md`.

## How it works

The mod is many small tweaks that don't depend on each other. Each one has its own toggle in `PluginConfig`, and its code lives in a class in `Tweaks/` (registered from `Plugin.OnAwake`), a patch in `Patches/`, or both. Every patch class carries `[TimbnFeature(nameof(PluginConfig.X))]`, so when a game update breaks a patch only that tweak turns off and the rest keep running. Tweaks the game has since fixed are removed from the code.

- `StrayTechPoints` (`CollectStrayTechPoints`) pulls a tech point orb to the player when it settles off the walkable ground.
- `SermonResultDataPatch` (`SermonFaithRounding`) swaps the game's `Math.Round` on ceremony faith for one that snaps the float error first, so Priest's 3.5 doesn't round down.
- `StuckCarriers` and `ZombieWgoDataStuckCarriersPatch` (`StuckCarriers`) send a supplier zombie with nowhere to put its item back to its station with the no storage icon.
- `StuckResourceWorkers` and `ZombieWgoDataStuckResourceWorkersPatch` (`StuckResourceWorkers`) free the work spots a dismantled stand left taken, including ones already lost in a save.
- `NestedZoneRemoval` and `RemovePointerPatch` (`RemoveInWholeArea`) allow removing a building inside the build area when the game counts it as part of another zone.
- `ConveyorLoopCrash` and `ConveyorComponentPatch` (`ConveyorLoopCrash`) stop the crash from a conveyor loop feeding another loop. `HighlightConveyorLoops` draws arrows and a red square on the belt to remove.
- `BuiltEarlyQuests` (`BuiltEarlyQuests`) finishes the choir and organ quest steps when the building already exists, checked on load and when the step starts.
- `UnreachableDismantle` with `PlayerWorkComponentPatch` and `WgoDataPatch` (`DismantleUnreachable`) lets the player work on a station blocked by a fence or wall.
- `LostItems` (`SoftLockedBoards` and `LostQuestItems`) adds Larry talks through Core's `Dialog.AddTalk` that give back two boards or the Inquisitor's medallion when the player is stuck without them.
- `MoveIdenticalFromBags` and `InventoryPatch` (`MoveIdenticalFromBags`) make the chest window's move identical button take from bags too.
- `SortIntoBags` with `ItemPatch` and `InventoryUIItemMoveOpHandlerPatch` (`SortIntoBags`) put new items into the bag made for them.
- `Unstuck` (`UnstuckKey` and `UnstuckRange`) moves the player to the nearest open ground, run from `Plugin.OnUpdate`.
- `BedSaveWhenRested` (`BedSaveWhenRested`) saves through Core's `Events.SleepRefused` when the keeper is too rested to sleep.
- `TechPointCap` (`TechPointCap`) raises the red, green and blue caps with Core's `Balance.Edit<GameResSystemDef>`, only while the setting is above the game's 999.

## Config

Keys sit in three sections, `Bug Fixes`, `Soft Locks` and `Tweaks`. Everything is on by default except `BedSaveWhenRested` and `TechPointCap` (999, the game's cap). `UnstuckKey` defaults to Ctrl+U and clearing it turns the key off, and `UnstuckRange` is 1 to 100 world units (default 30). `HighlightConveyorLoops` needs `ConveyorLoopCrash`. `TimbnConfig.CarryOver` moves values over from the old section and key names, so don't rename a key.

## Translating

Every line the mod adds is in `lang/en.txt`. Copy it to the game's language id, such as `lang/de.txt`, `lang/fr.txt` or `lang/pt-br.txt`, and translate the text after each `=`. Lines left out show in English.

## Building

It needs Timbn Core 2.2.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead.
