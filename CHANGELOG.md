# Changelog

## [2.0.0] - 2026-09-05

Full rebrand from **Heavypin** to **Sledgepin** (breaking identity / install / BepInEx GUID).

### Changed

- Plugin folder: `BepInEx/plugins/AGR-75-Sledgepin/`
- Assembly / bundle: `AGR75Sledgepin.dll`, `AGR75Sledgepin.nobp`
- BepInEx GUID: `com.mursisru.agr75sledgepin`
- Blueprinter keys: `missilepack_agr75sledgepin`, mounts `MissilePack_AGR75Sledgepin_4x` / `_6x`
- Display / encyclopedia name: **AGR-75 Sledgepin**
- Unity visuals / bake assets renamed to `Sledgepin*`

> [!CAUTION]
> **Breaking:** remove the old `AGR-75-Heavypin` plugin folder before installing. Hangar loadouts / NOMNOM packs keyed to the old GUID or json keys will not carry over automatically.

> [!IMPORTANT]
> Requires **BepInEx 5** and **[Blueprinter 2.0.1+](https://github.com/nikkorap/NOBlueprinter-Releases)** (`com.nikkorap.blueprinter`).

## [1.1.0] - 2026-09-04

Balance and identity pass after player feedback (vs AGR-24 Kingpin).

### Added

- **IR-cold coast:** after motor burnout, all IR sources are stripped so IR SAMs lose the coasting rocket.
- Dedicated warhead apply path for live missile pierce / blastYield.

### Changed

- Warhead **16 kg HE**, pierce **420**, armor-tier effectiveness **4.5**.
- Motor: burn **5.5 s**, fuel **30 kg**, thrust **24 kN**, top speed **2.5 Mach**.
- Design / hangar range envelope **~18 km** (air-launch CalcProxy: 280 m/s @ 4 km).
- Glide: stock `finArea` for CalcRange; ApplyAero lift ×3 / drag ×0.85.
- Cost **0.14**.

### Fixed

- Hangar **AP / HE / R** no longer read shared AGR-24 `unitPrefab` (looked identical to Kingpin).
- Laser HUD range uses CalcProxy motors instead of unpatched stock prefab.

> [!IMPORTANT]
> Requires **BepInEx 5** and **[Blueprinter 2.0.1+](https://github.com/nikkorap/NOBlueprinter-Releases)** (`com.nikkorap.blueprinter`).

## [1.0.0] - 2026-09-01

First public release (published as AGR-75 Heavypin).

### Added

- AGR-75 laser-guided rocket for Nuclear Option (BepInEx 5 + Blueprinter 2.0.1).
- AGR-24 donor flight; 4× and 6× launcher mounts on aircraft/helicopter AGR slots.
- Embedded mount rockets, fin deploy animation, motor FX on four nozzles, Blender visual bundle.

### Fixed

- Glide aero scales with deployed fin area.
- Motor FX bind on live rocket; preview icon vertical squeeze.

### Changed

- Blueprinter 2.0.1: bootstrap waits `PatchRunner.ApplyAllOps` (replaces `PatchingComplete`).

> [!IMPORTANT]
> Requires **BepInEx 5** and **[Blueprinter 2.0.1+](https://github.com/nikkorap/NOBlueprinter-Releases)** (`com.nikkorap.blueprinter`).
