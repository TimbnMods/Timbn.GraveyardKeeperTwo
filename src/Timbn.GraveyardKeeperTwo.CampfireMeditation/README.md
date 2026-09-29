# Timbn.GraveyardKeeperTwo.CampfireMeditation

Timbn Campfire Meditation lets you pass time by a campfire, like the stone garden in the first game.

## How to use

Stand by a campfire, like the one at the Inquisition Base, and a "Meditate" hint appears above it. Press interact to start meditating, and press interact again ("Wake Up") to stop.

## What happens

- The screen fades to black, you turn to face the fire where you stand, and the view fades back in as if it were midnight with the sun, moon and ambient light gone dark, so the fire and the other lamps around light the scene, day or night. The HUD stays up.
- Time runs 7 times faster, so a day passes in about 43 seconds.
- You slowly get energy back, a quarter of what sleep gives, with the usual +1 energy popup over your head.
- Your insanity slowly goes down too, by up to 20 each time you meditate, the same as a night's sleep.
- The world keeps running at that speed, so zombies and crafts carry on.
- Getting up fades to black and back to normal.

## Good to know

- The game is not saved while you meditate.
- Going back to the main menu ends a meditation in progress.

## Translating

Every line this mod adds is in `lang/en.txt` inside its folder. Copy it to your game's language id, such as `lang/de.txt`, `lang/fr.txt`, or `lang/pt-br.txt`, and translate the text after each `=`. The game shows your translation when it runs in that language, and English for any line you leave out.

## Requirements

- Timbn Core 1.4.1 or newer

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.CampfireMeditation.cfg`

| Section | Entry | Default | What it does |
| --- | --- | --- | --- |
| General | Enabled | true | Turns the mod on or off. |
| Meditation | TimeSpeed | 7 | How many times faster time runs while you meditate (1 to 50, sleep runs at 50). |
| Meditation | EnergyPerDay | 100 | Energy regained per in game day of meditating (0 to 400, sleep gives 400). |
| Meditation | InsanityPerDay | 40 | Insanity removed per in game day of meditating (0 to 400). |
| Meditation | MaxInsanity | 20 | The most insanity one meditation can remove (0 to 100, sleep removes 20). |
| Meditation | Radius | 2 | How close to a campfire you have to stand. |
