# Changelog

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
