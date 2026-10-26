# Timbn.GraveyardKeeperTwo.CampfireMeditation

Timbn Campfire Meditation lets players sit by the campfire at home and pass time, like the stone garden in the first game. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/CampfireMeditation/page.yml`, and what changed in each version is in `CHANGELOG.md`.

## How it works

- `Plugin` hooks `Meditation.Tick` to Core's `Events.Update` and `Meditation.Stop` to `Events.SaveClosed`, so leaving a save ends a meditation.
- The home campfire is scenery with no action of its own, so `Meditation` registers its spot with `Interactions.Register` and puts `Meditate` on the primary key. The label comes back empty while the keeper is tired, which hides it.
- While the keeper meditates, `UI.ShowKeyHints` shows Back at the bottom of the screen, and Back wakes them. Sitting takes a hold with `Player.TakeControl`, fades with `UI.FadeThrough`, turns the keeper to face the fire and speeds time with `Clock.MultiplyGameSpeed`.
- Each tick it adds energy with `TimbnPlayer.AddEnergy` and removes insanity with `TimbnPlayer.AddInsanity`, scaled by the day fraction that passed. Getting tired or a UI taking control ends it.
- `MeditationLighting` applies the time of day preset closest to midnight as the `EnvironmentEngine` override and puts the old override back after. `EnvironmentEnginePatch` runs `Darken` after every `ApplyPreset` to zero the sun and ambient light, since the game reapplies the preset as the clock moves. Darkening only happens while the override is still midnight, so a game script that changes it wins.

## Config

- `TimeSpeed` is how many times faster time runs (7 by default).
- `EnergyPerDay` is energy regained per in game day.
- `InsanityPerDay` is insanity removed per in game day.
- `MaxInsanity` is the most insanity one meditation removes.
- `Radius` is how far past the keeper's reach the fire still counts.

## Translating

Every line the mod adds is in `lang/en.txt`. Copy it to the game's language id, such as `lang/de.txt`, `lang/fr.txt` or `lang/pt-br.txt`, and translate the text after each `=`. Lines left out show in English.

## Building

It needs Timbn Core 2.2.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead.
