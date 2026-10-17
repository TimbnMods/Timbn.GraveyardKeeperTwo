# Timbn Graveyard Keeper 2 Mods

A suite of [BepInEx](https://github.com/BepInEx/BepInEx) mods for Graveyard Keeper 2 (Lazy Bear Games). Core hosts the Timbn framework, and the other mods build on it.

[![NuGet Version](https://img.shields.io/nuget/v/Timbn.GraveyardKeeperTwo.Core)](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Core)

## Mods

| Mod | What it does |
| --- | --- |
| **[Core](src/Timbn.GraveyardKeeperTwo.Core/README.md)** | Shared framework the other mods depend on, which makes modding easier. |
| **[CampfireMeditation](src/Timbn.GraveyardKeeperTwo.CampfireMeditation/README.md)** | Pass time by a campfire, like the stone garden in the first game. |
| **[ChestLabels](src/Timbn.GraveyardKeeperTwo.ChestLabels/README.md)** | Name your chests. The name floats over the chest in the same style as grave quality. |
| **[InfusionRack](src/Timbn.GraveyardKeeperTwo.InfusionRack/README.md)** | Gives your zombies tech points. Lay them in infusion stands and run Spirit Solutions made from organs on the infusion machine. |
| **[LivingTown](src/Timbn.GraveyardKeeperTwo.LivingTown/README.md)** | Gives the village and the port a daily life, with Herm, Linda and Larry going about their day. |
| **[QuestPacks](src/Timbn.GraveyardKeeperTwo.QuestPacks/README.md)** | Adds quests and talks written as plain JSON files, no code needed. Install it for any quest pack, or for a mod that ships one. |
| **[VanillaTweaks](src/Timbn.GraveyardKeeperTwo.VanillaTweaks/README.md)** | Aims to fix bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. |
| **[WorkshopSync](src/Timbn.GraveyardKeeperTwo.WorkshopSync/README.md)** | Installs the BepInEx mods you subscribe to on the Steam Workshop. A BepInEx patcher that goes in BepInEx/patchers and does not need Core. |
| **[ZombieCustomizer](src/Timbn.GraveyardKeeperTwo.ZombieCustomizer/README.md)** | Restyle your zombies' hairstyle, hair color and clothes color in the game's own customization window. |

The mods target Graveyard Keeper 2 (Unity 6, Mono, 64-bit) on BepInEx 5.4.x, the x64 Mono build. Players install them from Nexus, Thunderstore or the Steam Workshop, and each mod's player page is written in `docs/<Mod>/page.yml`. The READMEs in this repo are for developers.

Every mod has a General / Enabled toggle. Core's toggle is the master switch, so turning Core off turns off every Timbn mod.

## Making your own mod

Build on Core from the [Timbn.GraveyardKeeperTwo.Core](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Core) NuGet package. The quickest start is the [Timbn.GraveyardKeeperTwo.Templates](https://www.nuget.org/packages/Timbn.GraveyardKeeperTwo.Templates) template.

```cmd
dotnet new install Timbn.GraveyardKeeperTwo.Templates
dotnet new gk2mod -n MyMod
```

[Core's README](src/Timbn.GraveyardKeeperTwo.Core/README.md) and the [docs site](https://timbnmods.github.io/) cover the rest.

## Building from source

Requires the .NET SDK. Game DLLs are referenced from lib and from your Graveyard Keeper 2 Managed folder (via the GameDir property in src/Directory.Build.props, adjust if the game is installed elsewhere).

To deploy a mod to the plugins folder:

```cmd
dotnet build src/Timbn.GraveyardKeeperTwo.VanillaTweaks/Timbn.GraveyardKeeperTwo.VanillaTweaks.csproj -c Debug -p:DeployTo=plugins
```

The framework supports [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) with no setup needed from mod authors.

To deploy a mod to the scripts folder while the game is not running:

```cmd
dotnet build src/Timbn.GraveyardKeeperTwo.VanillaTweaks/Timbn.GraveyardKeeperTwo.VanillaTweaks.csproj -c Debug -p:DeployTo=scripts
```

To deploy a mod to the scripts folder while the game is running:

```cmd
dotnet build src/Timbn.GraveyardKeeperTwo.VanillaTweaks/Timbn.GraveyardKeeperTwo.VanillaTweaks.csproj -c Debug -p:DeployTo=scripts -p:BuildProjectReferences=false
```

## Releasing

A GitHub Action does the packaging and uploads, and only the DLL is built locally.

1. Merging a PR that changes a mod drafts a GitHub release for it, when its `CHANGELOG.md` has a section for the csproj version and that version has no tag yet.
2. Build the mod in Release and attach the DLL to the draft (Core also attaches its XML doc file).
3. Run the Release drafts workflow. It packages every draft that has its DLL, shows the zips and page text in the run summary, then waits for approval.
4. Approving uploads to Nexus, Thunderstore, the Steam Workshop and (for Core) NuGet, then publishes the GitHub release.

The packaging and page rendering live in `tools/release/Release.cs`. To check a mod's zips and page text locally:

```cmd
dotnet run tools/release/Release.cs -- VanillaTweaks --dry-run
```

The Sync pages workflow updates the Nexus and Steam page text from `page.yml` without a release.
