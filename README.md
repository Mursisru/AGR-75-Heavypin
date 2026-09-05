# AGR-75 Sledgepin

[![Version](https://img.shields.io/badge/version-2.0.0-blue.svg)](./CHANGELOG.md)
[![BepInEx](https://img.shields.io/badge/BepInEx-5-green.svg)](https://github.com/BepInEx/BepInEx)
[![License](https://img.shields.io/badge/license-All%20Rights%20Reserved-lightgrey.svg)](#)

BepInEx 5 mod for **Nuclear Option**: AGR-75 Sledgepin laser-guided rocket pack (AGR-24 Kingpin donor).

> [!IMPORTANT]
> **Install** into `Nuclear Option/BepInEx/plugins/AGR-75-Sledgepin/` (`AGR75Sledgepin.dll`, `AGR75Sledgepin.nobp`, `AGR-Preview.png`, `Textures/AGR75/`).

> [!NOTE]
> Hangar: **4x** on small AGR slots, **6x** on large. Aircraft and helicopters only.

## Features
- Shared AGR-24 `unitPrefab`; visuals stamped after spawn
- Own 4x / 6x launchers with dummy snap (ROOT only)
- Laser seeker HUD from AGR-24
- Stats: 75 kg, 16 kg HE, high AP, 5.5 s burn then IR-cold coast, 2.5 Mach, ~18 km design range

## Build
- Plugin: `dotnet build AGR75Sledgepin/AGR75Sledgepin.csproj -c Release`
- Bundle: Unity **2022.3.62f3** → `BatchBuild.Build` → `AGR75Sledgepin.nobp`

## License
All rights reserved unless otherwise stated by the author (Mursisru).
