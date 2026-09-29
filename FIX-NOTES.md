# Stardew Valley 1.6 prediction fix

**Author:** Igor Henrique ([@igorhrmoraes](https://github.com/igorhrmoraes))

**Version:** 4.2.0-unofficial.20260928

**Verified environment:** Stardew Valley 1.6.15.24356, SMAPI 4.5.2, Generic Mod Config Menu 1.16.0.

## Root cause: a different overnight random sequence

The previous fork initialized its prediction generator with a sum:

```csharp
Utility.CreateRandom((int)Game1.uniqueIDForThisGame / 100 + num3 * 10 + 1 + steps);
```

That does not match `Game1._newDayAfterFade` in the tested game version. With the new 1.6 RNG, hashing a single summed value is different from hashing separate inputs. The game also passes the intermediate seed through `Utility.CreateRandom`, which applies another seed-creation step. The corrected initialization is:

```csharp
int num3 = (int)(Game1.stats.DaysPlayed + 1);
int overnightSeed = Utility.CreateRandomSeed(
    Game1.uniqueIDForThisGame / 100,
    num3 * 10 + 1,
    steps
);
Random random = Utility.CreateRandom(overnightSeed);
```

Removing the early `int` cast also preserves the game ID before division. Both the input boundaries and the second seed-creation step matter: an otherwise correct simulation starting from the wrong seed predicts the wrong dish and luck.

## Keeping the RNG calls in sync

Matching the seed alone is insufficient. Every relevant random draw before daily luck must occur in the same order as the game.

The simulation follows the next day, including the month rollover from day 28 to day 1. It consumes the initial day-based draws, rejects forbidden Saloon dishes, rolls dish stock and consumes the item-creation draw before the later overnight operations.

The compatibility update corrects these additional sources of divergence:

- **Machines:** `Utility.ForEachLocation` includes interiors. Objects without a location or held output, Auto-Grabbers, sprinklers and already-ready products are excluded. Remaining processing time respects `ShouldTimePassForMachine`; the simulation counts the applicable RNG call for machines still working overnight according to their machine data and `WorkingEffects`.
- **Cursed mannequins:** RNG consumption is conditioned on the current location and on the farmer wearing both a shirt and pants, matching the relevant farmhouse, island farmhouse and shed conditions.
- **Stale state:** cached location and prediction data are invalidated once per second in addition to relevant input and step changes, allowing time and machine-state changes to update the prediction.
- **Verification:** `DayEnding` stores the final prediction and `DayStarted` compares it with the actual base luck, dish ID and stock. A mismatch is logged as a warning.

Prediction and step searches use a separate `Random` instance. Looking up dish names reads item metadata instead of creating items, avoiding accidental RNG consumption in the display and configuration menu.

## Configuration menu

Optional Generic Mod Config Menu integration adds panel position, width and scale, minimum base luck, desired dish, minimum dish stock, search range, visibility options and a toggle key. Saving or resetting settings invalidates prediction caches. The mod also works without GMCM.

The luck slider uses integer thousandths before converting to `double`. This prevents floating-point rounding above +0.100 from making the maximum-luck target impossible to satisfy. Dish choices use the game's eligible dish range and exclusions, display localized names and retain language-independent internal names in configuration.

Legacy configuration values are preserved, including comma-separated dish targets and the disabled-luck sentinel. Invalid numeric settings are normalized. The panel stays within the viewport and reduces its effective scale when needed to fit its height. English and Portuguese translations cover the menu and new HUD messages.

## Validation

The corrected prediction was checked over multiple days in game. One recorded overnight comparison was:

```text
Night check PASS: raw luck 0.100/0.100; dish 222/222; amount 1/1
```

That line confirms all three outputs for that night; it does not establish correctness for every possible save state. The menu was also checked in game.

The published source retains the same mod logic as the tested local build. Publication checks include:

- A release build against the installed Stardew Valley and SMAPI assemblies.
- Legacy configuration preservation, default values and invalid-value recovery.
- All seven interface signatures used against the installed GMCM assembly.
- 81 combinations of viewport, width, scale and position with synthetic font metrics.
- Matching English and Portuguese translation placeholders.

The source project accepts an explicit `GamePath`, and the repository includes a reproducible check runner. Only the mod DLL is distributed; game and SMAPI assemblies are not redistributed.

## Remaining limitations

Overnight lightning before 23:00 is not simulated and may shift the RNG sequence on stormy nights. The HUD warns about this case. The predictor is single-player only; other mods that alter overnight RNG and unmodeled machine behavior can also affect results. Base luck excludes the Special Charm bonus. Weather and fairy events are outside this mod's scope.

Original code and MIT license by BlaDe; visibility toggle by Versy024. The compatibility fix and configuration menu in this fork are by Igor Henrique.
