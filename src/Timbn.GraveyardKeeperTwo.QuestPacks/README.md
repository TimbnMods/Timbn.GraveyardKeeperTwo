# Timbn.GraveyardKeeperTwo.QuestPacks

Timbn Quest Packs adds quests and talks written as plain JSON files, no code needed. Install it for any quest pack, or for a mod that ships one.

## What happens

- A quest pack is a folder anywhere in `BepInEx/plugins` with a `pack.json` and a `quests` folder. Its quests show up in the game like any other.
- A mod can ship a `quests` folder next to its DLL, and those quests come and go with the mod.
- Edit a standalone pack while the game runs, and it's read again the next time you're back on the main menu.
- Mistakes in a pack file are written to the log, and the rest of the pack still loads.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| `General:ShowProblemsOnMainMenu` | off | Shows a popup on the main menu listing mistakes found in quest pack files. |

Needs Timbn Core.
