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
            "Heavy laser-guided air-to-ground rocket. Four-nozzle motor, 11.5kg HE, 12-18km depending on launch speed.";

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
        public const float BlastYieldKg = 11.5f;
        public const float Cost = 0.12f;
        public const float RadarSize = 0.015f;

        public const float LengthM = 1.36f;
        public const float WidthM = 0.17f;
        public const float HeightM = 0.17f;
        public const float VisualUniformScale = 1f;
        public const float MountEmptyMass4Kg = 18f;
        public const float MountEmptyMass6Kg = 24f;

        public const float DesignRangeM = 15000f;
        public const float EncyclopediaMinRangeM = 400f;
        public const float CalcRestLaunchSpeedMps = 0f;
        public const float CalcRestLaunchAltM = 0f;
        public const float CalcRestTargetAltM = 0f;
        public const float CalcRestTargetDistM = 15000f;
        public const float Pk = 0.45f;

        public const float MotorThrustN = 20000f;
        public const float MotorFuelKg = 16f;
        public const float MotorBurnS = 2.5f;
        public const float DesignTopSpeedMach = 2.2f;
        public const float SeaLevelSpeedOfSoundMps = 340f;
        public const float DesignTopSpeedMps = DesignTopSpeedMach * SeaLevelSpeedOfSoundMps;

        // Missile.ApplyAero: lift and drag scale with finArea / currentFinArea.
        public const float GlideFinAreaScale = 2.5f;

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
