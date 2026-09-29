# Timbn.GraveyardKeeperTwo.WorkshopSync

Timbn Workshop Sync installs the BepInEx mods you subscribe to on the Steam Workshop. Steam downloads Workshop items into its own folder and the game never loads code from there, so without this you copy each mod into BepInEx by hand and again after every update.

## How it works

Every time the game starts, before any mod loads, it looks through your subscribed Graveyard Keeper 2 Workshop items and installs them.

- A mod you just subscribed to loads on that same start.
- When Steam updates a mod, the new files are installed on the next start.
- When you unsubscribe, the mod is removed on the next start.
- Items that aren't BepInEx mods, such as translations, are left alone.

## Good to know

- Workshop mods are made by other players and run with the same access as any other mod. Only subscribe to mods you trust.
- Workshop mods go in `BepInEx/plugins/Workshop`, which is emptied and filled again every start, so don't put anything of your own in it.
- A config file a Workshop mod comes with is only added when you don't have that file yet, so your own settings are never overwritten, and it stays when you unsubscribe.
- An update Steam downloads while the game is running applies when you restart the game.
- If you also installed the same mod by hand, BepInEx loads the newer version and warns about the other.

## Install

BepInEx has to be installed by hand once, since the Workshop can't install it.

1. Install [BepInEx 5](https://www.nexusmods.com/graveyardkeeper2/mods/48) into your Graveyard Keeper 2 folder.
2. Extract the zip into your Graveyard Keeper 2 folder, so the mod ends up in `BepInEx/patchers/Timbn.GraveyardKeeperTwo.WorkshopSync` (not `plugins`). Vortex does this for you.
3. Launch the game.

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.WorkshopSync.cfg`

| Section | Entry | Default | What it does |
| --- | --- | --- | --- |
| General | Enabled | true | Turns the mod on or off. While off, Workshop mods already installed stay as they are. |
| General | SkippedItems | (empty) | Workshop items to leave out, as ids separated by commas. The id is the number at the end of the item's Steam page address. A skipped item is uninstalled, apart from any config files it added. |
| General | SteamFolder | (empty) | The Steam folder your Workshop downloads are in, the one that holds `steamapps`, such as `C:\Program Files (x86)\Steam`. Leave it empty to use the Steam library Graveyard Keeper 2 is installed in. |

## For mod authors

Put your files in folders named `plugins` and `config`. They are found at any depth in your item, and everything inside them is installed.

| Folder | Installed to |
| --- | --- |
| `plugins` | `BepInEx/plugins/Workshop/<item id>` |
| `config` | `BepInEx/config`, only files the player doesn't have yet |

Everything else in your item is ignored, so a readme or preview image next to them is fine. A DLL has to be inside a `plugins` folder to be installed.

Either of these layouts works.

```
My Workshop item                   My Workshop item
└─ BepInEx                         ├─ README.md
   ├─ plugins                      ├─ plugins
   │  └─ MyMod                     │  └─ MyMod.dll
   │     └─ MyMod.dll              └─ config
   └─ config                          └─ MyMod.cfg
      └─ MyMod.cfg
```

The first matches the game folder, so players without Workshop Sync can copy it in by hand.
