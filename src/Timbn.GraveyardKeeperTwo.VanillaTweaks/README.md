# Timbn.GraveyardKeeperTwo.VanillaTweaks

Timbn Vanilla Tweaks fixes bugs and adds light gameplay tweaks that are intended to enhance the vanilla experience without cheating. Almost every fix and tweak can be turned off on its own in the config.

## Bug Fixes

### Study Table eating your science

If all storage in the tower is full and the study table has no science in it, a zombie can put its item in the table. The table is then unusable and every science you make afterwards is lost. This puts a science only filter on the table so that can't happen, and anything already stuck in the table is moved out when you load.

### Study table stopping partway

A broken popup on the workstation can stop the study table partway through. Pressing Study could take your faith and science and start nothing, and pressing Decompose could leave the table stuck. Both now are fixed and a table already stuck is freed when you load.

### Green Thumb perk

The perk never worked and now adds +2 farming mastery when planting. The game does not use the player's mastery on harvest, so that part of the description still can't do anything.

### Crematorium bodies no longer get stuck

Putting a body in the crematorium with 0 Anatomy mastery made the crematorium unusable. It now refuses the body until you have Anatomy 1, and any body already stuck is burned when you load the game.

### Lost tech points

Tech points can bounce through a wall and land where they can't reach you. Now any tech points stuck off walkable ground are pulled to you.

### Zombie cooks losing ingredients

Ovens only had room for one ingredient, so zombies threw away the second ingredient of any two ingredient recipe and tried to deliver again. Ovens now have room for every ingredient, and a delivery with no room is refused instead of destroyed.

### Priest perk rounding down

The game can end up with 3.4999 faith when it should be 3.5, which rounds down instead of up, so Priest adds nothing at some parishioner counts. Faith now rounds on the intended value.

### Stuck supply zombies

Supply zombies could freeze holding an item when all storage was full. They now walk back to their station and show the game's storage full icon until there is room.

## Soft Locks

### Quest building built too early

Building the church choir before Agatha asks for it will lock the quest from completing. The game only noticed the build at the moment it happened, not if it has happened in the past. The quest now finishes when you load a save with the choir or organ already built.

## Vanilla Tweaks

### Unstuck hotkey

No more getting wedged into a tight spot. A configurable key (default Ctrl+U) moves you to the nearest open ground.

### Bed saving without being tired

Using the bed saves the game even if you are not tired enough to sleep.

### Higher tech point cap

Raises the red, green, and blue tech point cap from 999 to 9999. Set it to 999 in the config to keep the game's cap.

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.VanillaTweaks.cfg`

| Section | Entry | Default | What it does |
| --- | --- | --- | --- |
| Quests | BuiltEarly | true | Finishes quests that are stuck because you built an upgrade before a NPC asked for it. |
| Carriers | ShowWhenStuck | true | When a carrier zombie has nothing in its room to put its item into, it shows the no storage icon the game already uses for gardeners and walks back to its supplier station instead of freezing where it " stands. It goes on with its work as soon as a slot frees up, as it would anyway. |
| Unstuck | Key | CTRL + U | Moves you to the nearest open ground when you are boxed in, say between chests or inside a collider. Only works with no window open. Clear it to turn the key off. |
| Unstuck | Range | 30 | How far Unstuck may move you, in world units |
| TechPoints | Cap | 9999 | The most red, green, or blue tech points you can hold. The game caps each at 999 and throws away " anything past it. Set to 999 to keep the game's cap. |
| StudyTable | ScienceOnly | true | Lets the study table hold only science. Without it, a zombie putting items into the tower's storage can fill the table's one slot, where the table window never shows them, and every science you make " afterwards is lost. Anything already stuck in the table is moved to your inventory when a save loads. Turning " this off takes the filter back off your tables when a save loads. |
| GreenThumb | TalentBonus | true | Makes the Green Thumb perk grant its +2 green talent when planting. The game puts the value in a field that planting crafts throw away, so the perk does nothing at all. Does nothing once the game " fixes it. |
| Crematorium | NoStuckBodies | true | The crematorium refuses a body until you have Anatomy 1, the level its burn needs. The game takes the body anyway, fails to start the burn, and leaves it stuck for good. A body already stuck when a " save loads is burned. |
| TechPoints | CollectStray | true | Pulls a tech point orb to you when it settles off the walkable ground, say through a wall, where the game's magnet can never reach it. The game only does this when you sleep. |
| Oven | NoLostIngredients | true | Gives the oven room for every ingredient its recipes need. It has one ingredient slot, so a zombie cook's second ingredient (the oil for onion rings, say) has nowhere to go and is destroyed on " delivery, over and over. Also refuses any delivery a station has no room for, instead of " destroying it. |
| Sermons | FaithRounding | true | Rounds ceremony faith the way the numbers say. Priest makes Basic Prayer pay 0.35 faith per parishioner, but 0.35 is stored as 0.3499999, so at 10, 30, 50 parishioners and so on 3.5 comes " out as 3.4999999 and rounds down, and Priest adds nothing. |
| Bed | SaveWhenRested | true | Saves the game when you use the bed with full energy and the Keeper refuses to sleep. The game only saves when you actually sleep.