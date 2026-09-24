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

### Buildings in the resurrection lab can't be removed

Anything you built in the resurrection lab from the morgue build desk could never be removed. This fixes the issue.

## Soft Locks

### Quest building built too early

Building the church choir before Agatha asks for it will lock the quest from completing. The game only noticed the build at the moment it happened, not if it has happened in the past. The quest now finishes when you load a save with the choir or organ already built.

## Vanilla Tweaks

### Unstuck hotkey

No more getting wedged into a tight spot. A configurable key (default Ctrl+U) moves you to the nearest open ground.

### Bed saving without being tired

Using the bed saves the game even if you are not tired enough to sleep. This is off by default. Turn on Bed SaveWhenRested in the config to use it.

### Higher tech point cap

The game caps red, green, and blue tech points at 999 each and throws away anything past that. This is off by default. Raise TechPoints Cap in the config (up to 999999) to hold more.

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.VanillaTweaks.cfg`

| Section | Entry | Default |
| --- | --- | --- |
| Quests | BuiltEarly | true |
| Building | RemoveInWholeArea | true |
| Carriers | ShowWhenStuck | true |
| Unstuck | Key | CTRL + U |
| Unstuck | Range | 30 |
| TechPoints | Cap | 999 |
| StudyTable | ScienceOnly | true |
| GreenThumb | TalentBonus | true |
| Crematorium | NoStuckBodies | true |
| TechPoints | CollectStray | true |
| Oven | NoLostIngredients | true |
| Sermons | FaithRounding | true |
| Bed | SaveWhenRested | false |