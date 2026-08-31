using System.Reflection;

namespace Heavypin.Runtime
{
    internal static class HeavypinDefinitionMass
    {
        private static readonly FieldInfo? NullableMass =
            typeof(MissileDefinition).GetField("mass",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        internal static void Apply(MissileDefinition? def, float kg)
        {
            if (def == null || kg <= 0f)
                return;
            NullableMass?.SetValue(def, kg);
        }
    }
}
