# Timbn.GraveyardKeeperTwo.Core

Timbn Core is the shared framework every Timbn mod for Graveyard Keeper 2 is built on. Install it when a Timbn mod lists it as a requirement. Modders can build on it too, see For Modders.

## Requirements

- [BepInEx for Graveyard Keeper 2](https://www.nexusmods.com/graveyardkeeper2/mods/48)

## Installation

### With Vortex

1. Install BepInEx for Graveyard Keeper 2
2. Download with "Mod Manager Download" from the Files tab or the Vortex download button

### Without Vortex

1. Install BepInEx for Graveyard Keeper 2, start the game once, and quit at the main menu. This creates the `BepInEx/plugins` and `BepInEx/config` folders.
2. Download the Timbn.GraveyardKeeperTwo.Core zip file.
3. Extract the zip into `BepInEx/plugins`. You should end up with `Graveyard Keeper 2/BepInEx/plugins/Timbn.GraveyardKeeperTwo.Core/`.

### Check it works

The bottom right of the main menu lists Timbn Core along with any other Timbn mods.

## For Modders

To start fast, run `dotnet new install Timbn.GraveyardKeeperTwo.Templates` once, then `dotnet new gk2mod -n MyMod`. It makes a project with everything below already set up. Otherwise add the [`Timbn.GraveyardKeeperTwo.Core`](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Core) NuGet package to your project. It points at the game's DLLs in your Graveyard Keeper 2 install (set `GameDir` if the game is somewhere else) and never copies Core next to your plugin, since players get Core as its own mod. Then inherit `TimbnFrameworkPlugin<T>` on your BepInEx plugin class, where T is that class. It patches your assembly, binds your config, and gives you logging and per frame updates.

```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void OnAwake()
    {
        Logger.LogInfo("Hello from my plugin");
    }
}
```

The plugin's properties are how you add things to the game. `Quests.Add` adds quests with offers, hand ins and stages that chain like the game's own, `Dialog.AddTalk` gives an NPC a conversation, and `Npcs.Schedule` and `Npcs.Find` give people a day of their own or walk them somewhere for a quest, along the paths, over the walkable ground, or in a straight line for a short step. `Stations.Add` puts a new workstation with your own 3D model on a build desk, and `Crafts.Add` adds a recipe at any station, yours or the game's, with their names in your lang files or in the call. `UnlockedBy` has a tech unlock a station or a recipe, `Techs.Add` puts a new tech of your own on the tech tree, and `Mastery` sets the tool level a recipe needs. `Models` loads your asset bundle for those and for extra parts like a headstone. `Audio.PlayMusic` plays one of the game's music tracks and `Audio.PlaySound` one of its sounds, such as `item_use_drink`. There are also events, settings, potions, text, sprites, balance changes and save data. People, groups, their events and their animations come from `TimbnGameIds.Npcs`, `.Groups`, `.NpcEvents` and `.NpcAnimations`, and `TimbnGameIds` also names the game's items, sounds, music, techs, perks, buffs, build desks and workstations. Everything a plugin adds is taken out again when it unloads. Items, buffs, recipes and stations a plugin adds are kept out of the game's save file and come back when the save loads, so removing the plugin later leaves a save that still loads.

## License

Mozilla Public License 2.0, see `LICENSE.txt`.
