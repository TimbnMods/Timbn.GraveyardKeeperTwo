# Timbn.GraveyardKeeperTwo.LivingTown

Timbn Living Town gives the village and the port a daily life.

## What happens

- Herm opens his stall at 6:00 and now and then wanders off for a short walk, to the pond, a bench or a chat with the guard, and comes back. When it rains he stays at his stall.
- At 16:48 he closes and goes to sit in the tavern.
- At 20:24 he heads home for the night.
- Outside his opening hours he won't trade and tells you to come back in the morning.
- Once you've finished the Safe Harbor quest, Linda works her club from 8:00, going between the tables.
- From 15:00 she waits by the club's entrance and now and then goes to see the cat.
- You can pet the cat on the box by the club's entrance too.
- From 18:00 she's back inside until she goes to bed at 2:00. Once you've finished Concept for a Concert, she spends the hours after midnight on the stage, and you hear her violin while you're in the club.
- Larry potters around his spot at your base and now and then walks over to the fairy spot and back.
- Once you've met the astrologer in the port, he now and then reads the necronomicon or looks over his pieces in his tower.
- Each day from 6:00 villagers and townsfolk come to visit the graveyard, one for every 5 points of town gratitude (`GratitudePerVisitor`), at least 2 (`MinimumChurchVisitors`) and up to 15 (`MaximumChurchVisitors`). A town district joins in once people live there.
- Half again as many come on Wrath, the sermon day (still no more than the maximum), and rain keeps some of them home.
- They arrive by the donkey until 13:00, alone or in twos and threes, and go their own ways once inside. Keeping to the paths, each one strolls, stops at a grave, monument, columbarium, crypt or flower bed, sits on a bench, or goes into the church to stand by a pew (except on Wrath). One person stops at each, two on a bench. Groups leave together the way they came 4 to 6 hours later, or at 16:00 at the latest. You can walk straight through them, so they never block your way.
- Saving sends away the ones who are there, so they're never written into your save. The rest of the day's visitors still come.
- The day after you fill in a new grave, there's a funeral for it. At least 10 villagers come, in small groups one close behind the other between 6:00 and 9:00, whatever the weather and even with `ChurchVisits` off. A church with few pews holds fewer, since nobody comes without a pew seat point or a speaker's place to stand at. They gather in the church, and by noon (sooner once everyone is in) the first three speak at the choir one after another, an hour for the first and half an hour each for the others. Then everyone follows those three out and lines up on the path to the grave, and each one looks at it for a few minutes before going home. By 15:00 they go out to the grave even if a speaker never got to speak. There's no funeral on Wrath, and a grave gets at most two tries, so sleeping through a funeral doesn't bring it back day after day.
- A story scene that needs any of them always comes first. They stop while it plays, and pick up their day again later.
- About once a week someone in the village, or in a part of town where people live again, asks you to cook a dish you know the recipe for. Say yes and "A Neighbor's Craving" shows up in your quest log, the only entry this adds, and it is reused for every request. Bring the dish to them and they pay its sale price. A request nobody cooks for within a week is dropped.
- About once a week someone gives you a keepsake, a box of vegetables or a healing potion to take to a friend, from the village to a part of town where people live again or the other way round. It shows up as "An Errand for a Neighbor", the only entry this adds for it, and the friend pays the item's sale price. The one it's for shows a speech bubble while you carry it. Nobody waits longer than a week, and if you took a parcel and didn't deliver it in time, you pay for it. It runs next to the cooking requests.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| `People:DailyRoutine` | on | Herm, Linda, Larry and the astrologer go about their day. Turning it off puts them back in their usual spots. |
| `People:OpeningHours` | on | Herm only trades from 6:00 until 16:48. Off lets you trade with him any time you find him. |
| `Crowds:ChurchVisits` | on | Villagers visit the graveyard each day, more the more grateful the town is. |
| `Crowds:GratitudePerVisitor` | 5 | How much town gratitude brings one more visitor, 1 to 50. |
| `Crowds:MinimumChurchVisitors` | 2 | The minimum graveyard visitors in a day, 0 to 30. |
| `Crowds:MaximumChurchVisitors` | 15 | The maximum graveyard visitors in a day, 0 to 30. If it's below the minimum, the minimum wins. |
| `Crowds:Funerals` | on | The day after you fill in a new grave, the villagers hold a funeral for it, even with `ChurchVisits` off. |
| `Requests:VillagerRequests` | on | About once a week someone in the village or town asks you to cook a dish you know, and pays its sale price. |
| `Requests:VillagerDeliveries` | on | About once a week someone gives you something to take between the village and town, and the friend pays its sale price. It runs out after a week. |
| `Debug:ShowGraveyardPaths` | off | Draws the paths graveyard visitors walk, where they stand and sit, and where each one is heading. |

## Translating

Every line this mod adds is in `lang/en.txt` inside its folder. Copy it to your game's language id, such as `lang/de.txt`, `lang/fr.txt`, or `lang/pt-br.txt`, and translate the text after each `=`. The game shows your translation when it runs in that language, and English for any line you leave out.

## Requirements

- Timbn Core 2.0.0 or newer

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.LivingTown.cfg`
