<div align="center">

<img src="https://staticdelivery.nexusmods.com/mods/1303/images/55821/55821-1795282452-1056202694.jpg" alt="Stardew QOL Thumbnail" width="600" />

# Stardew QOL

[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-Stardew%20QOL-orange?style=for-the-badge)](https://www.nexusmods.com/stardewvalley/mods/55821)
[![SMAPI](https://img.shields.io/badge/SMAPI-4.0.0%2B-blue?style=for-the-badge&logo=github)](https://smapi.io/)
[![Stardew Valley](https://img.shields.io/badge/Game-Stardew%20Valley-green?style=for-the-badge)](https://www.stardewvalley.net/)
[![GMCM](https://img.shields.io/badge/Optional-Generic%20Mod%20Config%20Menu-purple?style=for-the-badge)](https://www.nexusmods.com/stardewvalley/mods/5098)

</div>

---

## Description

**Stardew QOL** is a modular and highly customizable quality-of-life mod for Stardew Valley. Designed to adapt to your playstyle without breaking game balance, it gives you full control over time passage, automatic energy and health regeneration, and dynamic weather frequency. Every module and individual feature can be toggled or fine-tuned in-game at any time.

*Stardew QOL tries its best to match your pace.*

---

## Main Features

### Time Control Module
* **Custom Clock Speed:** Adjust how many real-world seconds pass per 10 in-game minutes (from the default 7 seconds up to 720 seconds for much longer, relaxing days). Higher values make the clock slower, while lower values make it faster.
* **24-Hour Real-Time Cycle:** Play with a true 1:1 real-time day cycle (overrides custom clock speed).
* **Museum Library Fast-Forward:** Speed up the clock (1s to 6s per 10 in-game minutes, where fewer seconds means faster clock speed and more seconds means slower than 1s) while sitting at the small study tables in the lower area of the Museum Library before 6:00 PM. Includes an optional **24h Balance** mode that adjusts the remaining day's clock speed to compensate for time skipped while seated.

### Automatic Energy Recovery
* **Activity-Based Modes:** Configure distinct regeneration amounts and intervals for *General Recovery*, *While Walking*, *While Standing Still*, *While Seated* (accelerates proportionally when using the Museum Library fast-forward), and *While Holding a Fishing Rod*. Only one mode runs at a time to prevent stacking.
* **Clock-Synced & Bypassable:** Energy recovery follows the in-game clock's seconds rather than real-world time—so the longer the 10-minute interval is, the slower energy recovers, and the faster the 10-minute clock ticks, the faster energy recovers. Players can even take advantage of this to bypass or speed up energy regeneration by making the 10-minute clock faster.
* **Action Cooldown:** Automatically pauses energy regeneration for 1 to 3 seconds after performing an action that consumes stamina.

### Automatic Health Recovery
* **Activity-Based Modes:** Independent regeneration settings for *General Recovery*, *While Walking*, *While Standing Still*, and *While Seated*. Fractional healing values smoothly accumulate until a full health point is restored.
* **Shared Damage Cooldown:** Taking damage temporarily pauses both Health and Energy regeneration (configurable from 1 to 3 seconds).

### Dynamic Climate Module
* **Additional Weather Rolls:** Adds configurable bonus chances (1% to 75%) for Rain and Snow on eligible sunny days without replacing or reducing the game's natural weather.
* **Balanced Caps:** Set maximum limits per half-month (resets on the 1st and 15th of each season). Calculated strictly by the in-game calendar so altered clock speeds never cause extra weather rolls.

### Full Customization & Localization
* **Master Toggle & Notifications:** Enable or disable the entire mod or individual modules on the fly without losing your saved settings. Optional HUD notifications let you know when automatic time acceleration starts or ends.
* **Languages Included:** Built-in support for English and Portuguese (PT-BR).

---

## Requirements

* **Required:** [SMAPI](https://smapi.io/) (4.0.0 or later).
* **Recommended (Optional):** [Generic Mod Config Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098) to configure all options easily in-game. If not installed, the mod can still be configured by editing `config.json`.

---

## Installation Instructions

1. Install the latest version of [SMAPI](https://smapi.io/).
2. Download **Stardew QOL** from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/55821) and extract the ZIP archive.
3. Move the `StardewQOL` folder into your `Stardew Valley/Mods` directory.
4. Launch the game using SMAPI.

---

## Shout Outs

* Thanks to **ConcernedApe** for creating Stardew Valley.
* Thanks to **Pathoschild** for SMAPI and the modding build tools.
* Thanks to **spacechase0** for Generic Mod Config Menu.
