# Timbn.GraveyardKeeperTwo.ChestLabels

Timbn Chest Labels lets players name their chests, and the name shows at the chest the way a grave's quality does. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/ChestLabels/page.yml`, and what changed in each version is in `CHANGELOG.md`.

## How it works

- `Plugin` adds the Label key to chests with Core's `Interactions.Register(...).AddSecondary`, which also draws the Label hint next to Open. Only objects whose id contains `chest` count, since `ChestInteractionHandler` also runs shelves, containers and storages.
- The key opens a Core page (`UI.ShowPage`) with a text box, a color picker, a size choice and a Save button.
- `WgoPatch` puts the name in the chest's bubble as a `UIQualityTooltipWidgetData`, the widget a grave's quality uses, so the game stacks it with the hints. It goes under them, or above them when `LabelBelow` is off. `UIQualityTooltipWidgetPatch` hides that widget's empty icon slot.
- The color is saved apart from the text and put around the name as a `<color>` tag when it is drawn.
- Core's `Events.WorldObjectRemoved` clears a name when its chest leaves the save for good. Core already skips an id change, a move to another scene and a removal that keeps the craft, so none of those clears it.
- A cleared name waits one second. If a mod moves the chest and sends Core's `moved` mod event in that time, the name goes to the new chest.

## Settings

- `Labels/LabelBelow` shows the name under the Open and Label hints (on by default) or above them.
- `Labels/Colors` is a comma separated list of colors, such as `#FF8800, orange`, that come before the default ones in the picker.
- `Labels/DefaultSize` is how big a name at the Default size is, a percent of the game's 16 unit hint text such as `150%` or a font size such as `24` (`100%` by default). A chest's own Small, Large or Extra Large size steps 2 units down, 2 units up or 4 units up from Default, so they all follow the setting.

## Save data

Names are registered with Core's `Saves.Register<ChestLabelData>`, keyed by the chest's `UniqueId`. They go into Core's Timbn save file next to the game's save, never the game's own save, so removing the mod leaves the save loading as normal.

## Translating

Every line the mod adds is in `lang/en.txt`. Copy it to the game's language id, such as `lang/de.txt`, `lang/fr.txt` or `lang/pt-br.txt`, and translate the text after each `=`. Lines left out show in English.

## Building

It needs Timbn Core 2.2.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead.
