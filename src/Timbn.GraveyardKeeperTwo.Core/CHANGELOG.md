# Changelog

## 2.2.1

- Works on game version 1.011.

## 2.2.0

- A new Mods window lists every installed mod with its settings. It opens from a Mod Settings button under the game's own Settings button, on the main menu and in the pause menu, and works with a mouse or a controller.
- Timbn mods can show an About card with a description and links, add pages of their own to the window, and share one line in the list as a group.
- Timbn Core is always on now, since every Timbn mod needs it.
- Timbn Core no longer hides other mods from tools such as RuntimeUnityEditor.
- New for mod authors are pages and popups in the game's own look, actions on the game's objects and on spots in the world, key hint bars, events mods send each other, the game's own words (OK, Cancel, short replies, NPC names) for any text or conversation line, translatable enum choices, pictures on pages, item, blueprint, perk and buff icons with one size rule each and the game's margin added for you, list cards a mod fills itself, rows that wrap onto new lines, and more helpers for the player, the world and quests.

## 2.1.0

- Mods can now open a window that asks for text, asks yes or no, or shows a message, in the game's own look. Chest Labels uses it to name chests.
- These windows load when a save starts, so the first one opens without a hitch.

## 2.0.1

- Saves load faster.
- No more stutter when many objects change at once, such as arriving somewhere new. Walkable area updates are spread over several frames.
- Fixed gaps in the walkable area where big objects like a riverbank cross two areas.
- Fixed a stutter at the start of a new day.
- Fixed a hitch on the first talk after loading.

## 2.0.0

- A big rebuild of the shared base every Timbn mod runs on.
- New for mod authors are custom stations and crafts, quests and talks, NPC walks, bubbles, storage windows, known recipe lookups, scene spot lookups, quest markers, hidden crafts that pay tech points, and villager looks loaded ahead of time.

## Older versions

The 1.x releases are listed on [GitHub](https://github.com/TimbnMods/Timbn.GraveyardKeeperTwo/releases).
