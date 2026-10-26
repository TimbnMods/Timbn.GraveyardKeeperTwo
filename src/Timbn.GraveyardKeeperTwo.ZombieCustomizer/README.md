# Timbn.GraveyardKeeperTwo.ZombieCustomizer

Timbn Zombie Customizer lets players restyle a zombie in the same customization window they use for the keeper. This README is for developers. The text players see on Nexus, Thunderstore and Steam comes from `docs/ZombieCustomizer/page.yml`, and what changed in each version is in `CHANGELOG.md`.

## How it works

- `UIZombieWorkerWindowPatch` has `RestyleButton` lay a clear button over the zombie's portrait (`workerIcon`) in its inspect window. Clicking it closes that window and opens `UICustomizationWindow` through `ZombieCustomization.Open`.
- `UICustomizationWindowPatch` hooks `Open`, `OnApplyPressed` and `Close`. While a zombie session is open, `ZombieCustomization` rebinds the hair, body and head color switchers to Core's `TimbnZombies.Looks()` pool, binds the first body color switcher to the clothes tint, and grays out the beard and other color rows.
- The preview reuses the keeper's `CustomizationCharacter` with a zombie skin preset from `ZombieSkinHelper`. It borrows a zombie head's color swap shader so the hair isn't raw magenta, and everything it changed is put back when the window closes, which then reopens the inspect window.
- Apply runs the game's `ZombieSkinHelper.ApplySkinToZombieWgoData` and redraws the zombie with `TimbnZombies.Redraw` instead of letting the window restyle the keeper.
- `ZombieTint` tints the `bdy`, `arm` and `leg` sprites, since zombies have only one outfit. Core's `Events.ZombieViewReady` repaints a zombie whenever its view is built, and `Events.SaveReady` and `SaveClosed` paint or reset every zombie.

## Config

- `ClothesColor1` to `ClothesColor10` in `Clothes Colors` are the tints offered after none. Changing one doesn't recolor zombies already styled, since each keeps its own color.

## Save data

The head, body and hair color go in the zombie's own game res values (`zombie_head_id`, `zombie_body_id`, `zombie_head_lut`, `zombie_body_lut`), the same ones the game sets, so they stay if the mod is removed. The tint is a hex string under `timbn_zombie_tint` in the zombie's `GameResStr`, which the game keeps but ignores without the mod.

## Building

It needs Timbn Core 2.2.0 or newer. A build deploys to `BepInEx/scripts` so F6 reloads it, and `-p:DeployTo=plugins` loads it at startup instead. It references `UnityEngine.UI` and `Sirenix.Serialization` from the game's Managed folder as well.
