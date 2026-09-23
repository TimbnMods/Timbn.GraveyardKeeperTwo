# Timbn Graveyard Keeper 2 Mods

A suite of [BepInEx](https://github.com/BepInEx/BepInEx) mods for Graveyard Keeper 2 (Lazy Bear Games). Core hosts the Timbn framework, and the other mods build on it.

## Mods

| Mod | What it does |
| --- | --- |
| **[Core](src/Timbn.GraveyardKeeperTwo.Core/README.md)** | Shared framework the other mods depend on that aims to makes modding easier. |
| **[VanillaTweaks](src/Timbn.GraveyardKeeperTwo.VanillaTweaks/README.md)** | Aims to fix bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. |

## Requirements

- Graveyard Keeper 2 (Unity 6, Mono, 64-bit)
- BepInEx 5.4.x, the x64 Mono build (not 6.x / IL2CPP)

## Installation

1. Install BepInEx 5 (x64, Mono) into your Graveyard Keeper 2 folder (next to GraveyardKeeper2.exe), then run the game once so it generates the BepInEx plugins and config folders.
2. Copy the plugin folders you want into BepInEx/plugins. Core is required by all the others.
3. Launch the game. Each configurable mod writes a file to BepInEx/config on first run.

## Configuration

Edit the files in BepInEx/config. BepInEx does not hot reload config, so restart the game after editing.

Every mod has a General / Enabled toggle (default true). Set it to false to turn that mod off while leaving it installed. Core's toggle is the master switch, so turning Core off turns off every Timbn mod.

## Building from source

Requires the .NET SDK. Game DLLs are referenced from lib and from your Graveyard Keeper 2 Managed folder (via the GameDir property in src/Directory.Build.props, adjust if Steam is installed elsewhere).

To deploy a mod to the plugins folder:

```cmd
dotnet build src/Timbn.GraveyardKeeperTwo.VanillaTweaks/Timbn.GraveyardKeeperTwo.VanillaTweaks.csproj -c Debug -p:DeployTo=plugins
```

The framework supports [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) with no setup needed from mod authors.

To deploy a mod to the scripts folder while the game is not running:

```cmd
dotnet build Timbn.GraveyardKeeperTwo.VanillaTweaks/Timbn.GraveyardKeeperTwo.VanillaTweaks.csproj -c Debug -p:DeployTo=scripts
```

To deploy a mod to the scripts folder while the game is running:

```cmd
dotnet build Timbn.GraveyardKeeperTwo.VanillaTweaks/Timbn.GraveyardKeeperTwo.VanillaTweaks.csproj -c Debug -p:DeployTo=scripts -p:BuildProjectReferences=false
```
