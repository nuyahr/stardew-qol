<div align="center">

<img src="https://staticdelivery.nexusmods.com/mods/1303/images/55821/55821-1795629853-286827511.png" alt="Stardew QOL Thumbnail" width="600" />

# Stardew QOL

[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-Download-orange?style=for-the-badge&logo=nexusmods)](https://www.nexusmods.com/stardewvalley/mods/55821)
[![SMAPI](https://img.shields.io/badge/SMAPI-4.0.0%2B-blue?style=for-the-badge&logo=github)](https://smapi.io/)
[![Stardew Valley](https://img.shields.io/badge/Game-Stardew%20Valley-green?style=for-the-badge)](https://www.stardewvalley.net/)
[![GMCM](https://img.shields.io/badge/Optional-Generic%20Mod%20Config%20Menu-purple?style=for-the-badge)](https://www.nexusmods.com/stardewvalley/mods/5098)

</div>

---

## Description

**Stardew QOL** is a modular and highly customizable quality-of-life mod for Stardew Valley focused on **gameplay rhythm and player freedom**. Built to let you choose your own pace without breaking game balance, it gives you full control over time passage, activity-based energy and health recovery, and dynamic weather frequency. Every module and individual feature can be toggled or fine-tuned in-game at any time so the game adapts seamlessly to how you like to play.

*Stardew QOL tries its best to match your pace.*

> **Note:** The stable release is designed for **Singleplayer**. An experimental **Multiplayer Beta** build is available exclusively here on **GitHub Releases** for testing and community feedback.

---

## Core Highlights

- **Choose Your Own Pace:** Fine-tune day length, play in 24-hour real time, or pass time in designated resting spots to enjoy the game at your preferred rhythm.
- **Smart & Balanced Recovery:** Restores Energy and Health based on your activity (standing, walking, sitting, or riding your horse) with real-world time intervals and action cooldowns to keep things fair.
- **Dynamic Climate Control:** Enjoy optional, calendar-balanced weather rolls without replacing standard game events.
- **Fully Modular:** Enable only the features you want using GMCM in-game or via `config.json`.

---

## Main Features

### Time Control Module
- **Custom Clock Speed:** Adjust how many real-world seconds pass per 10 in-game minutes (from 7.0s up to 720.0s, in 0.5s steps, for much longer, relaxing days). Higher values make the clock slower, while lower values make it faster.
- **24-Hour Real-Time Cycle:** Play with a true 1:1 real-time day cycle (overrides custom clock speed).
- **Resting Spots & Fast-Forward:** Speed up the clock while sitting at study tables in the Museum Library or other designated spots around the valley. Includes an optional **24h Balance** mode that adjusts the remaining day's clock speed to compensate for time skipped while seated.

### Automatic Energy Recovery
- **Activity-Based Modes:** Configure distinct regeneration amounts and real-world second intervals for *General Recovery*, *While Walking*, *While Standing Still*, *While Seated*, *While Mounted on Horse*, and *While Holding a Fishing Rod*. Only one mode runs at a time to prevent stacking.
- **Real-Time Intervals:** Recovery intervals run on real-world seconds rather than in-game clock speed, so regeneration timing stays consistent regardless of how fast or slow the day passes.
- **Action & Spa Cooldowns:** Automatically pauses energy regeneration for 2.0s to 5.0s after performing stamina-consuming actions, and intelligently pauses while inside the Spa pool to avoid stacking with vanilla healing.

### Automatic Health Recovery
- **Activity-Based Modes:** Independent regeneration amounts and real-world second intervals for *General Recovery*, *While Walking*, *While Standing Still*, *While Seated*, and *While Mounted on Horse*. Fractional healing values smoothly accumulate until a full health point is restored.
- **Shared Damage Cooldown:** Taking damage temporarily pauses both Health and Energy regeneration (configurable from 2.0s to 5.0s, in 1.0s steps).

### Dynamic Climate Module
- **Additional Weather Rolls:** Adds configurable bonus chances (1% up to 75% for Rain and Snow; 1% up to 35% for Storms) on eligible sunny days without replacing or reducing the game's natural weather.
- **Balanced Caps:** Set maximum limits per half-month (resets on the 1st and 15th of each season). Calculated strictly by the in-game calendar so altered clock speeds never cause extra weather rolls.

### Full Customization & Localization
- **Master Toggle & Notifications:** Enable or disable the entire mod or individual modules on the fly without losing your saved settings. Optional HUD notifications let you know when automatic time acceleration starts or ends.
- **Languages Included:** Built-in support for English and Portuguese (PT-BR).

---

## Requirements

- **Required:** [SMAPI](https://smapi.io/) (4.0.0 or later).
- **Recommended (Optional):** [Generic Mod Config Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098) to configure all options easily in-game. If not installed, the mod can still be configured by editing `config.json`.

---

## Installation Instructions

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download **Stardew QOL** from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/55821) (or GitHub Releases) and extract the ZIP archive.
3. Move the `StardewQOL` folder into your `Stardew Valley/Mods` directory.
4. Launch the game using SMAPI.

---

## Shout Outs

- Thanks to **ConcernedApe** for creating Stardew Valley.
- Thanks to **Pathoschild** for SMAPI and the modding build tools.
- Thanks to **spacechase0** for Generic Mod Config Menu.

---

## Author's Note

Thank you to everyone who downloads the mod ✨

It definitely isn't perfect—far from it—but I tried to give you freedom while keeping it from breaking the game or stripping away the purpose of each system inside it 😮‍💨.

Any feedback is very welcome so we can keep improving this 😵‍💫☝🏻. Bug reports or ideas to make it better are always appreciated 😸.

Please keep in mind that I might be away for a while depending on my personal situation 😖.

Still, thank you so much to everyone who downloads and plays the mod! I tried my best to make it comfortable without taking away the purpose of the original game—just trying to adapt a pace that fits each person.
