# Timbn.GraveyardKeeperTwo.InfusionRack

Timbn Infusion Rack gives zombies tech points. Players distill Spirit Solutions from organs, lay zombies in stands and run an infusion on the machine. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/InfusionRack/page.yml`, and what changed in each version is in `CHANGELOG.md`.

## How it works

The mod has no Harmony patches. Everything goes through Core's builders, registered from `Plugin.OnAwake`.

- `InfusionTech` adds the `timbn_infusion_rack` tech on the Anatomy tab after Body Places II. Every station and recipe names it in `UnlockedBy`.
- `SpiritSolution` adds the red, green and blue solution items, copied from `embalm_white` for its look and stacking but not an embalm, so the embalming table never offers them. `GiveTo` pays a zombie through `DoTechPointsReward`.
- `SpiritRecipes` adds one Distillation Cube recipe per organ that holds white skulls, based on the cube's fragrance. The organ list is hand written for 1.010 and needs a check after a game update.
- `InfusionMachine` is the station (based on the kitchen oven with no fuel, `Limit(1)`) with two stand slots. Its three crafts need a solution and a Blue Crystal, and only start while a stand holds a zombie.
- `InfusionStand` is built into a machine slot (`BuildsInSlot`) and holds one zombie (`HoldsZombie`, based on the resurrection table). The machine's craft starts a hidden stand craft on every idle stand, so nothing can be laid in mid infusion, and pays whoever is in each stand when it ends.
- The stands are read from the world at the end, not remembered, so an infusion running when the game saved still pays. A `timbn_infusion_done` resource on the stand lights the glow model, and `RepairEmptyStands` puts it out on `SaveReady` for a stand that lost its zombie.

## Config

- `PointsPerSolution` is the tech points each zombie gets from one solution (default 50).

## Save data

The mod saves nothing of its own. Core keeps its stations, items and crafts out of the game's save and in the Timbn save file, and a zombie in a stand drops to the ground if the mod is removed.

## Translating

Every line the mod adds is in `lang/en.txt`. Copy it to the game's language id, such as `lang/de.txt`, `lang/fr.txt` or `lang/pt-br.txt`, and translate the text after each `=`. Lines left out show in English.

## Building

It needs Timbn Core 2.2.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead. The art and icons in `Art/` and `Icons/` are embedded in the DLL.
