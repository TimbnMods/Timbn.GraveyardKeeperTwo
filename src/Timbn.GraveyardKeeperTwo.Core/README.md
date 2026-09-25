# Timbn.GraveyardKeeperTwo.Core

Timbn Core is the shared framework every Timbn mod for Graveyard Keeper 2 is built on. It does nothing on its own. Install it when a Timbn mod lists it as a requirement. The rest of this page is for modders.

Core is two things at once.

- A loaded plugin (`TimbnCorePlugin`) that other Timbn mods hard depend on.
- A shared framework (`TimbnFrameworkPlugin`) that every other Timbn mod builds on. The base plugin class gives you auto patching, config, logging, and per frame updates for free.

## Getting started

To make a Timbn plugin, just inherent `TimbnFrameworkPlugin<T>` on your BepInPlugin class where T is your mod class.

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
            new ConfigDescription(
                "The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away "
                + "anything past it. Set to 999 to keep the game's cap.",
                new AcceptableValueRange<int>(999, 999999)));
    }
}
```

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

## Main menu

Every Timbn plugin that starts is listed on the main menu with its version, above the game's own credits, so players can see at a glance what is loaded. `MainMenu.AddLine` adds another line to that list, and `MainMenu.Popup` shows a message in the game's own dialog window the next time the main menu is on screen. Core uses the popup to warn when the game version differs from the one these mods were tested on, and to list any mod that did not load. That covers every plugin BepInEx skipped over a missing, too old, or incompatible dependency, plus Timbn plugins that stayed off because a patch failed.

```csharp
protected override void OnAwake()
{
    MainMenu.Popup("Vanilla Tweaks", "The tech point cap is now 9999.");
}
```

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

### Notes

Each text field becomes a localization key named after the quest id (`<id>_offer_1`, `<id>_accept`, `<id>_give`, `quest_open_<id>_d`, and so on), so a translation can override any line.

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
        OnStart = () => SetPlayerSpeed(1.5f),
        OnEnd = () => SetPlayerSpeed(1f),
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