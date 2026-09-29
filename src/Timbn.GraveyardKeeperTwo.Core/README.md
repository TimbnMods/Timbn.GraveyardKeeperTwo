# Timbn.GraveyardKeeperTwo.Core

Timbn Core is the shared framework every Timbn mod for Graveyard Keeper 2 is built on. It does nothing on its own. Install it when a Timbn mod lists it as a requirement. Everything after For Modders is for modders.

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

## Settings

There is one setting, Enabled, in `BepInEx/config/Timbn.GraveyardKeeperTwo.Core.cfg`. Turning it off turns off every Timbn mod at once. Restart the game after changing it.

## Troubleshooting

If a game update changes something a Timbn mod relies on, that mod stays off instead of running half broken, and `BepInEx/LogOutput.log` says which part failed. If Core itself fails, every Timbn mod stays off. The startup line also warns when the game version differs from the one these mods were tested on, and a popup on the main menu says so once for each such game build. Include that line when reporting a problem.

If a mod does not load, a popup on the main menu names it and says why, for example when it needs a newer Timbn Core. Update the mod it names and restart the game.

## Uninstalling

Delete the `Timbn.GraveyardKeeperTwo.Core` folder from `BepInEx/plugins`. Every Timbn mod needs Core, so remove those too. Mods that keep their own data per save put it in a `<save>.TimbnSaveData.dat` file next to each of your saves, and data that belongs to no save goes in `TimbnGlobalData.dat` in the same folder. The game never reads them, so they are safe to leave or delete.

## License

Mozilla Public License 2.0, see `LICENSE.txt`.

## For Modders

`Timbn.GraveyardKeeperTwo.Core.xml` next to the DLL holds the API documentation for your IDE.

Core is two things at once.

- A loaded plugin (`TimbnCorePlugin`) that other Timbn mods hard depend on.
- A shared framework (`TimbnFrameworkPlugin`) that every other Timbn mod builds on. The base plugin class gives you auto patching, config, logging, and per frame updates for free.

## Getting started

To make a Timbn plugin, just inherit `TimbnFrameworkPlugin<T>` on your BepInPlugin class where T is your mod class.

```csharp
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(TimbnCorePlugin.Guid, "1.0.0")]
public class Plugin : TimbnFrameworkPlugin<Plugin>
{
    protected override void BindConfig(ConfigFile config) => PluginConfig.Bind(config);

    protected override void OnAwake()
    {
        Logger.LogInfo("Hello from my plugin");
    }

    protected override void OnUpdate()
    {
        // runs every frame while the plugin is enabled
    }
}

internal static class PluginConfig
{
    public static ConfigEntry<int> TechPointCap { get; private set; } = null!;

    public static void Bind(ConfigFile config)
    {
        TechPointCap = config.Bind(
            "TechPoints",
            "Cap",
            9999,
            "The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away "
            + "anything past it. Set to 999 to keep the game's cap.",
            999,
            999999);
    }
}
```

The last two values of that `Bind` are the lowest and highest allowed, which a config manager shows as a slider. It is Core's shortcut for BepInEx's `Bind` with an `AcceptableValueRange`. `Plugin.IsActive` says whether the plugin's own `Enabled` and Core's are both on, for a patch that has to stand down the moment a player turns the plugin off.

## Game Events

The framework provides an easy way to listen to game events from Graveyard Keeper.

```csharp
protected override void OnAwake()
{
    Events.GameStarted(OnGameStarted);
    Events.NewDayStarted(day => Logger.LogInfo($"day {day}"));
    Events.QuestCompleted(quest => Logger.LogInfo(quest.id));
}
```

Two events cover the whole life of a save, so there is no need to handle a hot reload by hand.

- `Events.SaveReady(handler)` runs for every save the player plays. It runs straight away when a save is already loaded, as it is when a plugin is hot reloaded mid game, and then on every `GameStarted`.
- `Events.SaveClosed(handler)` runs when the player stops playing a save, on the way to the main menu or when the plugin unloads mid game. It is the one place to put back what the plugin changed in the world, in place of the same cleanup in a `GoToMainMenu` handler and in `OnDestroyed`.

```csharp
protected override void OnAwake()
{
    var meditation = new Meditation(this);
    Events.Update(meditation.Tick);
    Events.SaveClosed(meditation.Stop);
}
```

A few events come from Core rather than the game.

- `Events.SleepStarted` and `Events.SleepEnded` run as the player falls asleep and wakes up.
- `Events.Trigger(type, handler)` runs when the game fires one of the story triggers its quests wait on, such as `GlobalEventsSystem.Event.Type.BuildBuilding`, with the trigger's id.
- `Events.BalanceLoaded(handler)` runs with the game's balance straight away if it has loaded, and after every later load.

Work that has to keep running can register for it too, so a tweak needs no `OnUpdate` of its own. `Events.Update(handler)` runs every frame, paused or not, and `Events.Every(seconds, handler)` runs at most that often and stops while the game is paused. Both run only while a save is loaded, and a handler that throws is logged once and stopped instead of failing every frame.

```csharp
Events.Every(0.5f, () => stuckCarriers.Tick());
```

## Settings that apply live

`Settings.Toggle` ties a config entry to the change it makes, so a setting switched in a config manager applies straight away instead of on the next start. While the setting is on, apply runs on every save load and as soon as it turns on. While it is off, revert runs on every save load and as soon as it turns off, and revert also runs when the plugin unloads. Apply can run more than once and revert can run when nothing was applied, so both must cope with that. At the main menu revert only runs for a change that was applied, so it can safely touch the loaded save. Leave revert out for a fix that runs once per save.

```csharp
protected override void OnAwake()
{
    var greenThumb = new GreenThumbTalentBonus();
    Settings.Toggle(PluginConfig.GreenThumbTalentBonus, greenThumb.Apply, greenThumb.Revert);

    Settings.Toggle(PluginConfig.StudyTableNoStuckCrafts, StudyTableStuckCraft.ClearStuckCrafts);

    var techPointCap = new TechPointCap();
    Settings.Toggle(PluginConfig.TechPointCap, cap => cap > 999, techPointCap.Apply, techPointCap.Revert);
}
```

The last form works for any setting type. Any change to the value reverts and then applies again, so apply can read the new value. A setting only read where it is used, such as a check inside a patch, is live already and needs none of this.

`Settings.While` keeps any Core registration alive only while a setting is on, and disposes it as soon as the setting turns off. A tweak that is switched off then costs nothing, and its handlers need no check of the setting.

```csharp
Settings.While(PluginConfig.StuckCarriers, () => Events.Every(0.5f, stuckCarriers.Tick));
Settings.While(PluginConfig.RecoverBattleRewards, () => Events.SleepStarted(LostBattleRewards.Recover));
Settings.While(PluginConfig.TechPointCap, cap => cap > 999, () => Balance.Edit<GameResSystemDef>("tech_red", Raise, Lower));
```

`Settings.Changed(entry, handler)` runs your code with the new value whenever a setting changes, for anything Toggle and While do not fit.

BepInEx only reads a config file at startup, so a setting edited by hand is not seen until something reloads it. `TimbnConfig.ReloadAll()` re-reads the file of every loaded plugin, Timbn or not, and `TimbnConfig.Reload(plugin)` does one. Each changed value raises its `SettingChanged` event, so settings tied with `Settings.Toggle` apply straight away. Call it on the main thread. A key deleted from a file keeps its current value instead of going back to its default.

## Changing the game's definitions

`Balance.Edit<T>(id, apply, revert)` changes one of the game's own definitions, such as a perk, an item, or a craft, and puts it back when the plugin unloads. Apply runs straight away if the balance has loaded, and again on the fresh copy each time it loads, so the change survives a reload. Keep what revert needs from inside apply, and pair it with `Settings.While` to follow a toggle.

```csharp
var ticks = 0;
Settings.While(PluginConfig.PerkTalentBonus, () => Balance.Edit<PerkDef>(
    "perk_green_thumb",
    perk => { ticks = perk.craftStartTicks; perk.craftMasteryBonus = ticks; perk.craftStartTicks = 0; },
    perk => { perk.craftStartTicks = ticks; perk.craftMasteryBonus = 0; }));
```

`Balance.Add` puts in a definition of your own. Adding a `CraftDef` or `ItemDef` also rebuilds the game's craft lookups, and `TimbnBalance.RefreshCraftCaches()` does the same after a mod changed crafts or item groups in place. `TimbnBalance.SetIcon(item, iconId)` changes an item's icon in both places the game keeps it.

## Holds

A hold changes something the game or another mod may also change, and gives it back when its handle is disposed, the plugin unloads, or the player returns to the main menu. Several mods can hold the same thing at once, and the value from before the first hold comes back when the last one goes, unless the game changed it meanwhile.

- `Player.HoldStill()` holds the player in place. The game's own lock requests are kept and applied on release.
- `Player.TakeControl()` takes the player's control away the way the game's cutscenes do, under Core's own flag, so the game handing control back never frees the player early. Core's conversations use it too.
- `Player.SetSpeed(multiplier)` changes how fast the player walks, and `Clock.SetSpeed(multiplier)` how fast the game's time runs. The newest hold's speed wins.

```csharp
_fastTime = Clock.SetSpeed(7f);
// later
_fastTime?.Dispose();
```

## Helpers

- `TimbnConfig.CarryOver(config, (entry, oldSection, oldKey), ...)` moves the value of a renamed config entry into the new one. Call it at the end of `BindConfig`.
- `TimbnInput.CanUseHotkeys`, `PlayerHasControl`, `PlayerHasControlExcept(reasons)` and `GameHasInteraction` say when a mod's key should stay out of the way. `IsDownInGame`, `IsDownWithControl` and `IsDownInWindow<TWindow>` read a `KeyboardShortcut` with those checks built in.
- `TimbnUI.ActiveWindow`, `IsWindowOpen<TWindow>()` and `IsScreenFading` read the game's screens, and `TimbnUI.FadeThrough(action)` runs code while the screen is faded to black.
- `UI.CreateHint()` makes the game's "press a key to do something" hint for any spot in the world, and `UI.Gui(draw)` draws IMGUI in place of an `OnGUI` method. Both are taken down when the plugin unloads.
- `TimbnItems.CountAnywhere(id)` counts an item across the whole world and `CountOnPlayer(id)` only what the player carries, `GiveToPlayer` gives items and drops what does not fit in front of the player, `TakeFromPlayer` removes them, and `DropAt` and `PlayerDropPosition` drop items where the game would. `FindDrops(match)` finds items lying on the ground in every scene, and `RemoveDrops` clears them.
- `TimbnPlayer.Position`, `SceneId` and `Scene` say where the player is, and `MoveTo` moves them.
- `TimbnWorld.TryFindWalkable(point, range, out ground)` finds open ground near a spot, `TryGetNavGraph` gets a scene's navigation graph, `Near(position, radius)` lists the world objects around a spot, nearest first, and `ViewOf(data)` gets the view that draws one.
- `TimbnClock.Day`, `Weekday`, `TimeOfDay` and `Time` read the calendar and clock, and `Between(from, to)` measures time passed across midnight.
- `TimbnZombies.OnScene()` lists the zombies in the world, and `Skins()` the bodies, heads, and skin colors the game rolls for them.

## Main menu

Every Timbn plugin that starts is listed on the main menu with its version, above the game's own credits, so players can see at a glance what is loaded. `MainMenu.AddLine` adds another line to that list, and `MainMenu.Popup` shows a message in the game's own dialog window the next time the main menu is on screen. Core uses the popup to warn when the game version differs from the one these mods were tested on, and to list any mod that did not load. That covers every plugin BepInEx skipped over a missing, too old, or incompatible dependency, plus Timbn plugins that stayed off because a patch failed.

```csharp
protected override void OnAwake()
{
    MainMenu.Popup("Vanilla Tweaks", "The tech point cap is now 9999.");
}
```

## Save data

`Saves.Register<T>()` gives a plugin a data object that belongs to the loaded save. Register once in `OnAwake` and change `Current` whenever you like. Core reads it when a save loads, writes it each time the game saves, gives a new game a fresh `T`, and deletes it along with the save. Nothing goes inside the game's own save file, so removing a mod never breaks a save, and Steam Cloud syncs it along with the save.

```csharp
public sealed class GhostData
{
    public int DaysPassed { get; set; }
    public List<string> VisitedGraves { get; set; } = [];
}

private TimbnSaveData<GhostData> _saveData = null!;

protected override void OnAwake()
{
    _saveData = Saves.Register<GhostData>(data => Logger.LogInfo($"{data.DaysPassed} days so far"));
    Events.NewDayStarted(_ => _saveData.Current.DaysPassed++);
}
```

Every Timbn mod shares one file per save, `<slot>.TimbnSaveData.dat` next to the save itself in `AppData/LocalLow/Lazy Bear Games/Graveyard Keeper 2`. It holds JSON with a section per plugin GUID, each written with Newtonsoft.Json from the public properties and fields of `T`. The name has to end in `.dat` and sit next to the save, since Steam Cloud only syncs `*.dat` and `*.info` in that folder. Keep `T` to numbers, strings, lists, dictionaries, and plain classes of your own. Adding a member later is safe because an older file leaves it at its default, but renaming one loses what was saved under the old name.

- The game only saves when the player sleeps, so changes made after that are dropped if the player quits, the same as the game's own progress.
- The optional callback runs each time a save starts, before the plugin's `GameStarted` handlers.
- `Current` is a fresh `T` at the main menu. Anything written there is never saved.
- Each save keeps the previous file as `<slot>.TimbnSaveData.backup.dat`, and the new file is written as `.dat.new` first and then moved into place, so a crash while saving never leaves only half a file.
- A file that cannot be read is renamed to `.dat.bad` and Core loads the backup instead. A single section that cannot be read is kept as `.dat.<plugin GUID>.bad`, and only that plugin starts from a fresh `T`.
- A section whose plugin is not loaded, because it is turned off or removed, is kept as it is and comes back when the plugin does.
- On a hot reload the new copy of the plugin gets the old copy's data, unsaved changes included.

### Global data

`Saves.RegisterGlobal<T>()` is the same idea for data that belongs to no save, such as a tip the player already dismissed or a count over all their games. It works like `Saves.Register<T>()`, with `Current` the same in every save and at the main menu. Every Timbn mod shares one `TimbnGlobalData.dat` in the same folder as the saves, with the same JSON layout, backup, and `.bad` handling.

```csharp
var welcome = Saves.RegisterGlobal<WelcomeData>();
Events.GameStarted(() =>
{
    if (welcome.Current.Seen)
        return;

    welcome.Current.Seen = true;
    welcome.Save();
});
```

- Core writes it when the game saves, on the way to the main menu, when the game quits, and when the plugin unloads, and only when something changed. Call `Save()` right after a change that must survive a crash.
- Global data is not deleted with a save, and it is read once when the plugin starts, so the optional callback runs before `RegisterGlobal` returns.

## Quests, dialog, and text

Core can add quests, conversations, and localized text from code.

The easy way is a whole NPC quest in one `TimbnQuest` object. Core registers it, makes its text keys from the quest id, shows the NPC's speech icon when the quest is in the offer or ready to hand in state, and runs the conversations. The text fields hold the actual lines.

```csharp
Quests.Add(new TimbnQuest
{
    Id = "timbn_larry_wine",
    Npc = "npc_larry",
    Icon = "quest_icon_larry_portal",
    Name = "Larry's Thirst",
    Description = $"Larry wants 3 of wine. He insists it's medicinal.",
    CompletedDescription = "Larry got his wine. Most of it ended up on the floor, and he's having a lovely time.",
    Wants = [TimbnQuests.Item("grape_wine:1", 3)],
    RewardMoney = 300,
    //After = "timbn_larry_wine",
    AvailableWhen = () => TimbnQuests.StatusOf("6_intro_guards_burial") is QuestStatus.InProgress or QuestStatus.Completed,
    Offer =
    [
        "Oi, Keeper! C'mere, c'mere. *hic* You're my best mate, you are. Always have been.",
        $"Be a love and fetch us {bottles} of wine, would you? I'm a bit parched. Been parched for far too long, I have. *hic*",
    ],
    Accept = "Fine, I'll find you some wine.",
    Decline = "Not now, Larry.",
    Accepted = ["Cheers, guv! Proper stuff, mind. None of that vinegar the guards drink. Bloody hooligans."],
    Declined = ["Righto. I'll just stand here, then. Sober as a judge. *hic* A very sad judge."],
    Remind = ["Oi! Where's me wine? I've gone all... *hic* ...clear-headed."],
    Give = "Here's your wine.",
    Later = "Still working on it.",
    Thanks = ["Ohhh, lovely jubbly. Goes straight through me, but that's half the fun, innit?"],
});
```

### Quest Types

Quest types are determined by how you complete them, `Completion`.

Supported types right now are

- `Fetch` (default)
- `Custom`

A fetch quest completes when the player hands over `Wants` with the `Give` answer.

A custom quest is completed by your code with `quest.Complete()`, which also pays `RewardMoney`. It still gets the offer conversation, and the NPC says the `Remind` lines while it is in progress.

### Starting Quests

Supported ways to start a quest:

- `After` offers the quest only once another quest is completed and draws it as that quest's child in the tree.
- `AvailableWhen` adds any other condition, such as a story quest having started.

### More Control

There are underlying controls you can use if the generic quest server doesn't fit your needs. It is a `QuestDef` you build yourself.

```csharp
Text.Add(new Dictionary<string, string>
{
    ["my_quest"] = "My Quest",
    ["my_quest_give"] = "Here you go.",
});

var quest = TimbnQuests
    .CreateDefinition("my_quest")
    .FinishOnAnswer("my_quest_give", TimbnQuests.Item("wooden_plank", 5))
    .RewardMoney(200);

Quests.Register(quest);

Dialog.AddTalk("npc_larry", "my_quest_talk", () => TimbnQuests.CanFinish("my_quest"), talk => talk
    .Say("my_quest_remind")
    .Ask("my_quest_give", "my_quest_later")
    .If("my_quest_give", then => then.Say("my_quest_thanks")));
```

A conversation is a list of steps that each wait for the one before.

- `Say` and `PlayerSay` show a line in the NPC's or the player's bubble.
- `Ask` offers choices and `If` branches on the answer. A `TimbnAnswer` adds the game's own answer extras, so `Ask(new TimbnAnswer("key").Costs("flitch", 5))` shows an item price with a `0/5` style count. `Costs` and `CostsResource` take a price, `Requires` and `RequiresResource` show a lock for something the player must have (`RequiresHappiness` is the smiley a town vendor asks for, and `RequiresDay(TimbnWeekday.Pride)` limits an answer to one day of the week, and `RequiresOrder("quest_order_iron")` waits for a vendor order to be finished), and `Rewards` and `RewardsResource` show a reward on the right, with resources such as `money`. `ShowsReward` and `ShowsRewardResource` draw the same reward without giving it, for one your own step hands out. Each of these can be repeated on one answer, so an answer can cost or need several items. The answer is greyed out until the player can pay, and the game takes the price and gives the reward when it is picked. A plain string is an answer with no extras.
- `Do` runs your code, and `When(condition, then, otherwise)` branches on the game's state when the conversation reaches it.
- `Give("flitch", 2)` hands the player items (what does not fit drops at their feet), and `Take("flitch", 2)` removes items from the player without asking.

A line can carry values. `Say("larry_price", 5, "friend")` fills the game's `%1` and `%2` placeholders in the text, and a `Func<object>` value is worked out when the line is shown. A talk can be a one-shot. `AddTalk(..., new TimbnTalkOptions { Once = true })` stops the NPC offering it once the player has started it in that save, kept with the save's data like the rest of Core's per save state, and `Dialog.ResetOnceTalk("id")` offers it again. `TimbnTalkOptions` also takes a `BubbleColor` for one talk.

A line whose key has no text in the current language is logged once as a warning, since the game would show the raw key. A talk id has to be unique across NPCs.

### Translations

`Text.Add` text shows in every language. To let players translate a mod, put its text in files instead. Core adds them when the plugin starts, so there is no call to make. It reads every `lang/<language id>.txt` next to the mod's DLL (`en`, `de`, `fr`, `pt-br`, `es`, `ru`, `pl`, `ja`, `zh_cn`, `ko`, or `tr`), one `key = text` per line.

```text
# lang/en.txt
timbn_larry_boards_accept = Got any boards lying around?
timbn_larry_boards_given = Lucky for you, the Inquisitors left a couple behind my box.\nTry not to lose these too.
```

Lines starting with `#` are comments and `\n` is a line break. The game shows the file for its current language and falls back to `en.txt` for any key a translation lacks, so keep every key in `en.txt`. A translator copies it to `de.txt` and translates it, no rebuild needed. Ship the files with the mod from its csproj.

```xml
<ItemGroup>
  <None Include="lang\*.txt" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

### Notes

Each text field becomes a localization key named after the quest id (`<id>_offer_1`, `<id>_accept`, `<id>_give`, `quest_open_<id>_d`, and so on), so a translation can override any line.

**Game talks come first.** An NPC holds one talk per interaction, oldest first. Mod talks always wait behind the game's own, including ones the game adds later, and step aside while the NPC has a game quest ready to hand in, so they never delay the story. A mod talk shows a tinted copy of the game's speech bubble over the NPC, so players can tell it apart. It is gold by default, and each plugin can pick its own color with `Dialog.BubbleColor = new Color(0.6f, 0.8f, 1f);`.

**Placement is automatic.** Groups of quests that start fresh are packed into a band of rows at the top of the tree, and every game quest moves down by the band's height plus one spacer row. A group that hangs off a game quest instead goes in the nearest free spot below it, linked to every quest in that parent's cell (the game stacks several quests in one cell and shows only the latest). Positions are never saved, so the layout is simply redone whenever a quest registers or unregisters.

## Potions

Core can add potions that are real items with buffs or debuffs and show up in folio, can be mixed, and act like regular potions.

```csharp
Potions.Register(new TimbnPotion
{
    Id = "timbn_swift_and_sticky",
    Name = "Swift and Sticky",
    Description = "Makes you walk faster for a while, and drops a few sticks.",
    //Red Green Blue
    Runes = new Vector3Int(2, 0, 1),
    Buff = new TimbnPotionBuff
    {
        Name = "Swift",
        Description = "You walk faster.",
        GlowColor = () => "yellow",
        Seconds = () => 120f,
        OnAddExpressions = ["DropItem(\"stick\", 5)"],
        OnStart = () => _swift = Player.SetSpeed(1.5f),
        OnEnd = () => _swift?.Dispose(),
    },
});
```

### Status Affects

There are four ways to apply status affects:

- `OnAddExpressions` (From GK2)
- `OnRemoveExpressions` (From GK2)
- `OnStart` (Custom)
- `WhileActive` (Custom)

You can use the GK2 script expressions to apply a status affect using `OnAddExpressions` and `OnRemoveExpressions`. Some examples are:

```C#
DropItem("stick", 5)
UnlockCraft("jewelry")
IncreasePlayerInventory("5")
```

You can run custom code for status affects as well with `OnStart` and `WhileActive`.

**NOTE:** There is a difference on how we handle these, `OnStart` will run when the potion begins but will also run again if the game loads and a potion is active. The games `OnAddExpressions` will only run once and not on load.

### Customization

`IconId` can be any sprite the game has (potions use `i_pot_*`, buffs `b_*`), or one added with `Sprites.AddPng(name, pngBytes)`. PNGs smaller than the game's 48 by 48 icon canvas are centred on one.

Rune counts must be unique. The game gives each mix to the first formula whose runes match, so a potion sharing runes with an existing one would never work. Core refuses to add such a potion and logs why.