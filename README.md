# Daily Luck and Dish Predictions — Stardew Valley 1.6 Fix

Predict tomorrow's **base daily luck**, the Saloon's **dish of the day and stock**, and a total step count that meets your chosen targets. This fork fixes the overnight RNG simulation for Stardew Valley 1.6.15 and adds an optional Generic Mod Config Menu interface.

The 1.6 compatibility update and configuration menu are maintained by **Igor Henrique** ([@igorhrmoraes](https://github.com/igorhrmoraes)). Based on [BlaDe's original mod](https://github.com/Bla-De/StepsTakenRNGPredictionOnScreen) and [Versy024's visibility-toggle version](https://github.com/Versy024/StepsTakenRNGPrediction).

## What was broken?

The previous version added the game ID, next-day value and steps together before creating its random generator. Stardew Valley 1.6.15 instead hashes those inputs separately into an intermediate seed, then passes that seed through `Utility.CreateRandom`. Those operations produce different random sequences under the new RNG, causing incorrect luck and dish predictions. An early `int` cast could also truncate the game ID.

This update matches that initialization and corrects the simulated RNG consumption from machines and cursed mannequins. See [FIX-NOTES.md](FIX-NOTES.md) for the exact cause, implementation details, validation and remaining limitations.

## Install

Tested with **Stardew Valley 1.6.15.24356**, **SMAPI 4.5.2** and **Generic Mod Config Menu 1.16.0**, in single player.

1. Install [SMAPI](https://smapi.io/).
2. Download this repository using **Code → Download ZIP**, then extract it.
3. Copy only the `StepsTakenRNGPrediction` folder into your game's `Mods` folder. It includes the compiled mod, manifest, translations, default configuration and license.
4. Keep only one installed mod with the ID `BlaDe.StepsTakenOnScreen`. If updating an existing installation, back up its `config.json` and restore it after replacing the mod files to preserve your settings.
5. Optionally install [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098), then launch the game through SMAPI.

## Configure and use

Open **Daily Luck and Dish Predictions - 1.6 Fix** in Generic Mod Config Menu. Without GMCM, edit `config.json` and press **F5** in game to reload it.

| Setting | Behavior |
| --- | --- |
| Minimum base luck | −0.100 to +0.100 in steps of 0.001. +0.100 targets maximum base luck. The Special Charm bonus is excluded. |
| Desired dish | Select an eligible Saloon dish by its localized name, or choose any dish. |
| Minimum dish stock | 1–13; applies when a dish target is selected. |
| Step search limit | Tests 100–20,000 total step counts starting at your current count. Default: 1,000. Larger searches take more processing time. |
| Panel position | Horizontal and vertical offsets in UI pixels. |
| Panel size | Width of 240–1,200 pixels and scale of 50–200%. Height follows the content; the panel fits inside the viewport. |
| Visible information | Show or hide steps, luck, dish and mail gift predictions. |
| Toggle key | **F8** for new configurations. Existing configured keys are preserved. |

All enabled targets must match together. The displayed step target is the **total count to reach**, not the number of extra steps to take. A missing result means no match was found within the selected search range.

Existing settings remain supported, including comma-separated internal dish names in `TargetDish`, `TargetGifter`, and the legacy `TargetLuck: -1` value that disables the luck filter. Dish names are displayed in the game's language but stored as internal names. Menu and new HUD text are available in English and Portuguese.

The mod simulates its own random generator. It does not set your luck, grant items, change your step count or advance `Game1.random` while searching. Walking, time changes, machines and relationships can change the prediction, so check it near bedtime.

At bedtime the mod records its prediction. The next morning it logs `Night check PASS` or `Night check MISMATCH` with the expected and actual base luck, dish ID and stock.

## Limitations

- Single player only; predictions are disabled in multiplayer.
- During thunderstorms, sleeping before 23:00 can consume additional RNG calls from overnight lightning. That sequence is not simulated; the HUD displays a warning.
- Other mods that change overnight RNG consumption, and machine behavior outside the modeled conditions, can affect accuracy.
- This version does not predict rain or fairy events.

## Build from source

Use a .NET SDK supporting C# 12 (SDK 8 or newer), with the .NET 6 targeting pack available, and a local installation of Stardew Valley with SMAPI. The game and SMAPI assemblies are referenced locally and are not included in this repository.

From the repository root, set `GamePath` to the folder containing `Stardew Valley.dll`:

```powershell
dotnet build src/StepsTakenOnScreen/StepsTakenOnScreen.csproj -c Release -p:GamePath="C:/path/to/Stardew Valley"
```

The result is `src/StepsTakenOnScreen/bin/Release/net6.0/StepsTakenOnScreen.dll`. To refresh the installable folder, copy that DLL into `StepsTakenRNGPrediction`, replacing the existing mod DLL. Building does not modify your game installation.

## Run checks

The check runner requires the .NET 6 runtime and a local GMCM installation at `Mods/GenericModConfigMenu`. Build it, then run it from the repository root:

```powershell
dotnet build tests/ModChecks/ModChecks.csproj -c Release -p:GamePath="C:/path/to/Stardew Valley"
dotnet tests/ModChecks/bin/Release/net6.0/ModChecks.dll "C:/path/to/Stardew Valley"
```

Checks cover legacy settings and invalid values, the seven GMCM API signatures used by the mod, 81 panel-layout combinations using synthetic font metrics, and translation placeholders. They complement in-game overnight checks; they are not an exhaustive simulation of every possible night.

## Credits and license

Original mod by **BlaDe**, visibility toggle by **Versy024**, and this compatibility update and configuration menu by **Igor Henrique**. This is an unofficial fork. The original [MIT license](LICENSE) is retained and included in the installable folder.
