# Timbn.GraveyardKeeperTwo.WorkshopSync

The final version of Timbn Workshop Sync. The game installs Workshop mods itself from 1.011 on, so this version hands over to it and turns itself off. This README is for developers. The text players see comes from `docs/WorkshopSync/page.yml`.

## How it works

- It's a BepInEx preloader patcher, so it runs before the chainloader scans `plugins` and moved mods load on that same start.
- `WorkshopHandoff` moves each entry of `plugins/Workshop/<item id>` up into `plugins`, which is where the game installs them, and keeps any copy already there. It then deletes `plugins/Workshop`.
- It renames its own DLL to `.dll.retired`, since the game keeps its copying off while a DLL with `Timbn` in its name is in `patchers`.
- If a step throws, the DLL stays and the next start tries again.

## Building

It doesn't depend on Core. `BepInExPatcher` in the csproj makes it deploy to `BepInEx/patchers` and package there too, and it can't be hot reloaded.
