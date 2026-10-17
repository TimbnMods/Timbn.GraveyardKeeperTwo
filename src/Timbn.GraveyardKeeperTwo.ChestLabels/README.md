# Timbn.GraveyardKeeperTwo.ChestLabels

Timbn Chest Labels lets players name their chests, and the name floats over the chest the way a grave's quality does. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/ChestLabels/page.yml`, and what changed in each version is in `CHANGELOG.md`.

## How it works

- `ChestInteractionHandlerPatch` adds the Label hint next to Open on a chest.
- `WGOInteractionHandlerBasePatch` turns on the second interaction (`HasInteraction2`) for chests, and `Interact2` opens Core's `UI.ShowPrompt` to type the name.
- Only objects whose id contains `chest` can be labeled, since `ChestInteractionHandler` also runs shelves, containers and storages.
- `WgoPatch` puts the name at the top of the chest's bubble as a `UIQualityTooltipWidgetData`, the widget a grave's quality uses, so the game stacks it with the hints. `UIQualityTooltipWidgetPatch` hides that widget's empty icon slot.
- `WorldDataPatch` clears a name when its chest is taken apart. Changing an object's id removes it and adds it straight back with its craft kept, so that case is skipped.

## Save data

Names are registered with Core's `Saves.Register<ChestLabelData>`, keyed by the chest's `UniqueId`. They go into Core's Timbn save file next to the game's save, never the game's own save, so removing the mod leaves the save loading as normal.

## Translating

Every line the mod adds is in `lang/en.txt`. Copy it to the game's language id, such as `lang/de.txt`, `lang/fr.txt` or `lang/pt-br.txt`, and translate the text after each `=`. Lines left out show in English.

## Building

It needs Timbn Core 2.1.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead.
