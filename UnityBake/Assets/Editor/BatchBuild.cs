using System;
using UnityEditor;

public static class BatchBuild
{
    public static void Build()
    {
        Heavypin.UnityBake.NobpBundleBuilder.Build();
        if (string.Equals(Environment.GetEnvironmentVariable("HEAVYPIN_UNITY_EXIT"), "1", StringComparison.Ordinal))
            EditorApplication.Exit(0);
    }
}
