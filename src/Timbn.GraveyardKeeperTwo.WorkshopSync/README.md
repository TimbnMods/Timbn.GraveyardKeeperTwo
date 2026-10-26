# Timbn.GraveyardKeeperTwo.WorkshopSync

Timbn Workshop Sync installs the BepInEx mods a player subscribes to on the Steam Workshop, since the game never loads code from Steam's Workshop folder. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/WorkshopSync/page.yml`, and there is no `CHANGELOG.md` yet.

## How it works

- It's a BepInEx preloader patcher, not a plugin, so it runs before the chainloader scans `plugins` and a newly subscribed mod loads on that same start. `WorkshopSyncPatcher` patches nothing (`TargetDLLs` is empty) and only uses `Initialize` to run the sync.
- It doesn't use Core or Steamworks. `WorkshopMirror` reads the folders under `steamapps/workshop/content/4358690` directly. The Steam library is found two folders up from the game, by its `appmanifest_4358690.acf`, unless `SteamFolder` is set.
- `BepInEx/plugins/Workshop` is deleted and filled again every start, so updates and unsubscribes apply on the next start.
- In each item, every folder named `plugins` (at any depth) is copied to `plugins/Workshop/<item id>`, and every `config` folder to `BepInEx/config` without overwriting files the player already has. An item with neither, such as a translation, is left alone.
- An item that fails to copy is removed again so no half install is left.

## Config

- `General.Enabled` (on) runs the sync. While off, installed Workshop mods stay as they are.
- `General.SteamFolder` (empty) is the Steam folder holding `steamapps`, for a Workshop download that lives outside the game's library.
- `General.SkippedItems` (empty) lists item ids to leave out, separated by commas.

## Building

It doesn't depend on Core. `BepInExPatcher` in the csproj makes it deploy to `BepInEx/patchers` and package there too, and it can't be hot reloaded.
