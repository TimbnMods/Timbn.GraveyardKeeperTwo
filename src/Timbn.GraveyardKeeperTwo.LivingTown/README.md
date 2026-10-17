# Timbn.GraveyardKeeperTwo.LivingTown

Timbn Living Town gives the village and the port a daily life, with NPC routines, graveyard visitors, funerals and villager errands. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/LivingTown/page.yml`, and what changed in each version is in `CHANGELOG.md`.
## How it works

The mod has no Harmony patches. `Plugin.OnAwake` wires every part to Core's events and settings, and the code sits in feature folders under one namespace.

### Routines

- `HermDay`, `LindaDay`, `LarryDay` and `AstrologerDay` each build a `TimbnNpcDay` that `Npcs.Schedule` runs while `DailyRoutine` is on. Linda's and the astrologer's days wait on a finished quest (`OnlyWhile`).
- `LindaDay.Tick` plays the violin music while she is on the stage at night. The cat by the club is a `TimbnObjectAction` on `common_box_08`.
- `VendorHours` holds Herm's hours (6:00, 19:12 to match the game's stalls, 22:00). `Npcs.CloseTradeWhen` uses it to close his trade while `OpeningHours` is on.

### Visitors

- `ChurchVisits` plans the day's crowd (`TodaysCrowd`, from town gratitude and the three crowd settings, more on Wrath and fewer in rain) and spawns visitors with `Npcs.Spawn` at `donkey_01`, at most one per tick.
- `Prepare` runs on `SaveReady` and preloads the home groups' kinds with `Npcs.Preload` and warms the paths, so the first spawns don't hitch. `SendAway` runs on `BeforeSave`, so no visitor is ever written into a save.
- `GraveyardVisitors` drives each visitor from stop to stop until their leave time. Each stop is an `IGraveyardStop` (`StandByStop` for graves, monuments, columbariums, crypts and flower beds, `BenchStop`, `ChurchStop` and `WanderStop`) that reports its own `Capacity()`.
- `GraveyardPaths` turns the graveyard's road tiles into lines and junctions each morning. Every walk goes along them as one `WalkStraight`, favoring the cobbled main path and entering through the gate `gd_graveyard_weed_4`.

### Funerals

- `Funeral` is owned by `ChurchVisits` and runs from its tick, so it works with `ChurchVisits` off. The 2:00 check compares `TimbnWorld.GravesWithBodies()` to the known graves and holds a funeral for a new one.
- The first three mourners speak at the choir in turn, then everyone lines up along `GraveyardPaths.PointAlong` toward the grave. A grave gets at most two tries (`FuneralData.TriedGraves`).

### Errands

- `Requests` (cook a known dish) and `Deliveries` (carry a parcel between the village and a town district) are repeatable Core quests built with `Quests.Add`. Each rolls about once a week on `NewDayStarted` and expires after a week.
- `Areas` picks who can ask, from the village group and the town district groups, skipping anyone already in a quest.

## Config

- `People` `DailyRoutine` turns the four NPC days on, and off puts them back in their usual spots.
- `People` `OpeningHours` closes Herm's trade outside his hours.
- `Crowds` `ChurchVisits` turns the daily graveyard visitors on.
- `Crowds` `GratitudePerVisitor` is town gratitude per visitor (default 7).
- `Crowds` `MinimumChurchVisitors` and `MaximumChurchVisitors` clamp the crowd (default 2 and 30).
- `Crowds` `Funerals` turns funerals for new graves on.
- `Requests` `VillagerRequests` and `VillagerDeliveries` turn the two errands on.
- `Requests` `OfferMarkers` (off) and `HandInMarkers` (on) show the quest marker on the villager offering an errand and the one waiting for it.
- `Debug` `WriteDebugLog` is hidden and off. It writes every visit, walk and schedule decision to the log at the debug level.

## Save data

`FuneralData` is registered with Core's `Saves.Register<FuneralData>` and holds the known and tried graves. The errand quests are Core quests. Both go into Core's Timbn save file next to the game's save, never the game's own save.

## Translating

Every line the mod adds is in `lang/en.txt`. Copy it to the game's language id, such as `lang/de.txt`, `lang/fr.txt` or `lang/pt-br.txt`, and translate the text after each `=`. Lines left out show in English.

## Building

It needs Timbn Core 2.0.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead.
