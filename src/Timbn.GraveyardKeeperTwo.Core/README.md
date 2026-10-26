# Timbn.GraveyardKeeperTwo.Core

Timbn Core is the shared framework every Timbn mod for Graveyard Keeper 2 is built on, and other modders can build on it too. This README is for mod authors using the package. The text players see on Nexus, Thunderstore and Steam comes from `docs/Core/page.yml`, and what changed in each version is in `CHANGELOG.md`.

[![NuGet Version](https://img.shields.io/nuget/v/Timbn.GraveyardKeeperTwo.Core)](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Core)

## For Modders

To start fast, install the [`Timbn.GraveyardKeeperTwo.Templates`](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Templates) template once with `dotnet new install Timbn.GraveyardKeeperTwo.Templates`, then `dotnet new gk2mod -n MyMod`. It makes a project with everything below already set up. Otherwise add the [`Timbn.GraveyardKeeperTwo.Core`](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Core) NuGet package to your project. It points at the game's DLLs in your Graveyard Keeper 2 install (set `GameDir` if the game is somewhere else) and never copies Core next to your mod, since players get Core as its own mod. Then inherit `TimbnFrameworkPlugin<T>` on your BepInEx plugin class, where T is that class. It patches your assembly, binds your config, and gives you logging and per frame updates.

```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "2.2.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void OnAwake()
    {
        Logger.LogInfo("Hello from my mod");
    }
}
```

Everything Core offers, with guides and the API reference, is on the [docs site](https://timbnmods.github.io/).

## License

Mozilla Public License 2.0, see `LICENSE` in the repo.
