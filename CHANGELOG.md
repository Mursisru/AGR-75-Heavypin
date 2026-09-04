# Changelog

## [1.1.0] - 2026-09-04

Balance and identity pass after player feedback (vs AGR-24 Kingpin).

### Added

- **IR-cold coast:** after motor burnout, all IR sources are stripped so IR SAMs lose the coasting rocket (`HeavypinIrCold`).
- Dedicated warhead apply path (`HeavypinWarhead`) for live missile pierce / blastYield.

### Changed

- Warhead **16 kg HE**, pierce **420**, armor-tier effectiveness **4.5**.
- Motor: burn **5.5 s**, fuel **30 kg**, thrust **24 kN**, top speed **2.5 Mach**.
- Design / hangar range envelope **~18 km** (air-launch CalcProxy: 280 m/s @ 4 km).
- Glide: stock `finArea` for CalcRange; ApplyAero lift ×3 / drag ×0.85.
- Cost **0.14**.

### Fixed

- Hangar **AP / HE / R** no longer read shared AGR-24 `unitPrefab` (looked identical to Kingpin). TMP overrides + `WeaponInfo` fields.
- Laser HUD range uses CalcProxy motors instead of unpatched stock prefab.

> [!IMPORTANT]
> Requires **BepInEx 5** and **[Blueprinter 2.0.1+](https://github.com/nikkorap/NOBlueprinter-Releases)** (`com.nikkorap.blueprinter`).

## [1.0.0] - 2026-09-01

First public release.

### Added

- **AGR-75 Heavypin** laser-guided rocket for Nuclear Option (BepInEx 5 + Blueprinter 2.0.1).
- AGR-24 donor flight; 4× and 6× launcher mounts on aircraft/helicopter AGR slots.
- Embedded mount rockets, fin deploy animation, motor FX on four nozzles, Blender visual bundle.

### Fixed

- Glide aero scales with deployed fin area (`HeavypinAero`).
- Motor FX bind on live rocket; preview icon vertical squeeze.

### Changed

- Blueprinter 2.0.1: bootstrap waits `PatchRunner.ApplyAllOps` (replaces `PatchingComplete`).

> [!IMPORTANT]
> Requires **BepInEx 5** and **[Blueprinter 2.0.1+](https://github.com/nikkorap/NOBlueprinter-Releases)** (`com.nikkorap.blueprinter`).
