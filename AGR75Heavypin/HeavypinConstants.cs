namespace Heavypin
{
    internal static class HeavypinConstants
    {
        public const string MissileJsonKey = "missilepack_agr75heavypin";
        public const string MountKeyPrefix = "MissilePack_AGR75Heavypin_";
        public const string MountJsonKey4x = "MissilePack_AGR75Heavypin_4x";
        public const string MountJsonKey6x = "MissilePack_AGR75Heavypin_6x";
        public const string WeaponInfoName = "AGR-75 Heavypin";
        public const string MountDisplayName = "AGR-75 Heavypin";
        public const string UnitName = "AGR-75 Heavypin";
        public const string ShortName = "AGR-75";
        public const string BogeyName = "AGR-75 Heavypin";
        public const string SeekerTypeName = "Laser";
        public const string Description =
            "Heavy laser-guided air-to-ground rocket. 16kg HE, high AP, long burn then IR-cold coast. ~15-20km from air launch.";

        public const string RocketVisualName = "HeavypinRocket";
        public const string LauncherVisualName = "HeavypinLauncher";
        public const string LauncherEmbeddedRocketName = "Rocket";
        public const string BundleModName = "AGR75Heavypin";
        public const string NobpFileName = "AGR75Heavypin.nobp";
        public const string PreviewIconFileName = "AGR-Preview.png";
        public const string PreviewIconResource = "Heavypin.Resources.AGR-Preview.png";
        public const int PreviewIconAlphaBase = 255;
        public const int PreviewIconDarkLuma = 145;
        public const int PreviewIconSquareSize = 512;
        public const float PreviewIconVerticalScale = 0.9f;

        // Blender axis fix: launcher root + fly HeavypinRocket (embedded rockets inherit launcher yaw).
        public const float VisualMountYawDeg = 90f;
        public const float OpeningPlaybackRate = 4f;
        public const float LauncherLiftM = 0.05f;
        public const float LauncherForwardM = 0.20f;

        public const string Agr24Name = "AGR-24";
        public const string Agr24Alt = "Kingpin";
        public const string Agr18Name = "AGR-18";
        public const string Agr18Alt = "Lynchpin";

        public const int SmallAmmoMax = 7;
        public const int SlotCount4 = 4;
        public const int SlotCount6 = 6;

        public const float LaunchMassKg = 75f;
        // Distinct from AGR-24 Kingpin HE/AP (shared unitPrefab UI otherwise shows stock values).
        public const float BlastYieldKg = 16f;
        public const float PierceDamage = 420f;
        public const float ArmorTierEffectiveness = 4.5f;
        public const float Cost = 0.14f;
        public const float RadarSize = 0.015f;

        public const float LengthM = 1.36f;
        public const float WidthM = 0.17f;
        public const float HeightM = 0.17f;
        public const float VisualUniformScale = 1f;
        public const float MountEmptyMass4Kg = 18f;
        public const float MountEmptyMass6Kg = 24f;

        public const float DesignRangeM = 18000f;
        public const float EncyclopediaMinRangeM = 400f;
        // Air-launch envelope for hangar/encyclopedia R: (rest calc was ~5km and looked broken).
        public const float CalcRestLaunchSpeedMps = 280f;
        public const float CalcRestLaunchAltM = 4000f;
        public const float CalcRestTargetAltM = 0f;
        public const float CalcRestTargetDistM = 18000f;
        public const float Pk = 0.55f;

        public const float MotorThrustN = 24000f;
        public const float MotorFuelKg = 30f;
        // Longer burn = longer IR window; after burnout IR is stripped (cold coast).
        public const float MotorBurnS = 5.5f;
        public const float DesignTopSpeedMach = 2.5f;
        public const float SeaLevelSpeedOfSoundMps = 340f;
        public const float DesignTopSpeedMps = DesignTopSpeedMach * SeaLevelSpeedOfSoundMps;

        // ApplyAero postfix: lift × scale, drag × scale (finArea stays stock — CalcRange/coast).
        public const float GlideLiftScale = 3f;
        public const float GlideDragScale = 0.85f;

        public const string DummyRocketCenter = "CenterOfModel";
        public const string DummyLauncherCenter = "CenterOfModel";
        public const string DummyPylonAttach = "PlaceOfDocking";
        public const string DummySlot4Prefix = "CentarOfDockingAGRRocket";
        public const string DummySlot6Prefix = "PlaceOfDockingAGRRocket";
        public const string DummyNozzlePrefix = "EngineEffectsSpawn";

        public static readonly string[] AttachPylonAliases =
        {
            DummyPylonAttach, "DockingPlace", "PlaceOfRocketLock", "Attach_Pylon", "Pylon"
        };

        public static readonly string[] RocketCenterAliases =
        {
            DummyRocketCenter
        };

        public static readonly string[] SlotPrefixes =
        {
            DummySlot4Prefix, DummySlot6Prefix
        };

        public static readonly string[] ForeignOwnerTags =
        {
            "GpmTag", "WarewindTag", "CrosswimTag", "Mk54Tag", "HydraTag", "TorpedoTag"
        };
    }
}
