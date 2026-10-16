# Timbn.GraveyardKeeperTwo.InfusionRack

Timbn Infusion Rack gives your zombies tech points. Make Spirit Solutions from organs, lay zombies in infusion stands, and run an infusion on the infusion machine to pay each one the full points.

## How to use

1. Buy the Infusion Rack tech on the Anatomy tab, right after Body Places II. It costs 50 red, 50 green and 20 blue.
2. At the Distillation Cube, distill a heart, guts or brain that holds white skulls with 20 fire and an Alchemy Flasks I. You get one Spirit Solution for each white skull. A heart makes a Red solution, guts make Green and a brain makes Blue.
3. Build the Infusion Machine at the Morgue desk, then build an Infusion Stand into each of its two slots, one on each side.
4. Lay a zombie in each stand. Press interact with a zombie over your head to put it in, and press interact with free hands to take it back out.
5. Pick Red, Green or Blue on the machine. An infusion needs one solution of that color and one Blue Crystal, and it takes a short while.
6. When it finishes, every zombie lying in the stands gets the full points of that color.

## Good to know

- The machine only starts an infusion while a stand holds a zombie, so a solution and a crystal are never wasted on nothing.
- The points are the same however many zombies are in the stands.
- You can only build one machine.
- Taking the machine apart takes its stands too, and a zombie inside drops to the ground.
- A zombie in a stand is saved with your game, and an infusion running when the game saves still pays when you load.

## Translating

Every line this mod adds is in `lang/en.txt` inside its folder. Copy it to your game's language id, such as `lang/de.txt`, `lang/fr.txt`, or `lang/pt-br.txt`, and translate the text after each `=`. The game shows your translation when it runs in that language, and English for any line you leave out.

## Requirements

- Timbn Core 2.0.0 or newer

## Configuration

`BepInEx/config/Timbn.GraveyardKeeperTwo.InfusionRack.cfg`

- `PointsPerSolution` is how many tech points each zombie gets from one Spirit Solution (default 50).
