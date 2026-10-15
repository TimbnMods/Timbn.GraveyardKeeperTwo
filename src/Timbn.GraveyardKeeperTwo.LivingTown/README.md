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
- Larry wanders around inside the house.
- Once you've met the astrologer in the port, he now and then reads the necronomicon or looks over his pieces in his tower.
- Each day from 6:00 villagers and townsfolk come to visit the graveyard, one for every 7 points of town gratitude (`GratitudePerVisitor`), at least 2 (`MinimumChurchVisitors`) and up to 30 (`MaximumChurchVisitors`). A town district joins in once people live there.
- 50% more visitors come on Wrath, the sermon day (still capped by the maximum), while rain keeps some of them home.
- They arrive at the same spot at the donkey until 13:00, alone or in twos and threes, and go their own ways once inside. Keeping to the paths, each one strolls, stops at a grave, monument, columbarium, crypt or flower bed, sits on a bench, or goes into the church to stand by a pew (except on Wrath). One person stops at each, two on a bench. Groups leave together the way they came 4 to 6 hours later, or at 16:00 at the latest. You can walk straight through them, so they never block your way.
- Saving sends away the ones who are there, so they're never written into your save. The rest of the day's visitors still come.
- The day after you fill in a new grave, there's a funeral for it. At least 10 villagers come, in small groups one close behind the other between 6:00 and 9:00, whatever the weather and even with `ChurchVisits` off. A church with few pews holds fewer, since attendance is limited by available pew seats and speaker spots. They gather in the church, and by noon (sooner once everyone is in) the first three speak at the choir one after another, an hour for the first and half an hour each for the others. Then everyone follows those three out and lines up on the path to the grave, and each one looks at it for a few minutes before going home. By 15:00 they go out to the grave even if a speaker never got to speak. There's no funeral on Wrath, and a grave gets at most two attempts, so sleeping through a funeral doesn't repeat it indefinitely.
- A story scene that needs any of them always comes first. They stop while it plays, and pick up their day again later.
- About once a week someone in the village, or in a part of town that is restored, asks you to cook a dish you know the recipe for. Say yes and "A Neighbor's Craving" shows up in your quest log, the only entry this adds, and it is reused for every request. Bring the dish to them and they pay its sale price. A request left uncooked for a week expires.
- About once a week someone gives you a keepsake, a box of vegetables or a healing potion to take to a friend, from the village to a restored town district or vice versa. It shows up as "An Errand for a Neighbor", the only entry this adds for it, and the friend pays the item's sale price. The recipient shows a speech bubble while you carry it. Deliveries expire after one week, and if you accept a parcel without delivering it in time, you must pay for it. It runs next to the cooking requests.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| `People:DailyRoutine` | true | Enable to have Herm, Linda, Larry and the astrologer go about their day. Turning it off puts them back in their usual spots. |
| `People:OpeningHours` | true | Enable the vendors to be closed to trading outside of opening hours. |
| `Crowds:ChurchVisits` | true | Enable to have villagers and townsfolk visit the graveyard each day with crowd sizes scaling up with town gratitude. |
| `Crowds:GratitudePerVisitor` | 7 | Ratio of town gratitude to additional graveyard visitors. For example, every 8 gratitude points result in one extra visitor. |
| `Crowds:MinimumChurchVisitors` | 2 | The minimum number of villagers that visit the graveyard in a day, regardless of town gratitude. |
| `Crowds:MaximumChurchVisitors` | 30 | The maximum number of villagers that visit the graveyard in a day, regardless of town gratitude. |
| `Crowds:Funerals` | true | Enable villagers to hold funerals for bodies you recently buried. They gather in the church, three of them speak at the choir in turn, then everyone lines up at the grave to pay their respects. Funerals are postponed on sermon day. |
| `Requests:VillagerRequests` | true | Enable to have villagers in the village or town ask you to cook a dish you know the recipe for, and pay its sale price when you bring it. |
| `Requests:VillagerDeliveries` | true | Enable to have villagers in the village or town give you something to take between the village and town, and the friend pays its sale price. Deliveries expire after one week. |

## Translating

Every line this mod adds is in `lang/en.txt` inside its folder. Copy it to your game's language id, such as `lang/de.txt`, `lang/fr.txt`, or `lang/pt-br.txt`, and translate the text after each `=`. The game shows your translation when it runs in that language, and English for any line you leave out.

## Requirements

- Timbn Core 2.0.0 or newer

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.LivingTown.cfg`
