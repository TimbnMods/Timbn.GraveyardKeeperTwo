# Timbn.GraveyardKeeperTwo.QuestPacks

Timbn Quest Packs adds quests and talks written as plain JSON files, so a quest needs no code. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/QuestPacks/page.yml`, and there is no `CHANGELOG.md` yet.

## How it works

- `TimbnQuestPacks` finds the packs and loads them. A standalone pack is any folder under `BepInEx/plugins` with a `pack.json`, a `quests` folder and no DLL. A mod's pack is a `quests` folder next to a Timbn mod's DLL.
- Standalone packs belong to this mod. `LoadStandalone` runs on every `MainMenuOpened` and reloads them only when a file's size or write time changed.
- A mod's pack is put in that mod's own `Subscriptions` through `PluginStarted`, so it unloads and reloads with the mod. Its id is the mod's GUID unless it has a `pack.json`.
- `TimbnPackReader` turns the JSON into Core's `TimbnQuestBuilder` steps and talks, and `TimbnPackRules` reads the conditions, actions, values and events inside them. A broken entry is skipped and added to the problem list, so the rest of the pack still loads.
- Each step becomes a `TimbnQuestRuntime`, and talks go through `TimbnPluginDialog.AddRecorded`. Quest progress and once only talks are saved by Core, never by this mod.
- `CheckReady` runs every second and marks a quest ready once its `readyWhen` comes true.
- PNGs in a pack's `sprites` folder are added as `<pack id>_<name>`. A standalone pack's `lang` folder is read too.
- `TimbnPackReaderTests` in Core's test project covers the reader.

The pack format is defined by `pack.schema.json` and `quests.schema.json` here, which also let an editor check a file while it's typed.

## Config

- `General.ShowProblemsOnMainMenu` (off) shows a main menu popup listing the problems found in pack files. They always go to the log.

## Building

It needs Timbn Core 2.0.0 or newer. It always deploys to `BepInEx/plugins` and can't be hot reloaded, since it reaches Core's internals through `InternalsVisibleTo` and ScriptEngine renames the assemblies it loads.
