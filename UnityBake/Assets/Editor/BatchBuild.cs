using System;
using UnityEditor;

public static class BatchBuild
{
    public static void Build()
    {
        Sledgepin.UnityBake.NobpBundleBuilder.Build();
        if (string.Equals(Environment.GetEnvironmentVariable("SLEDGEPIN_UNITY_EXIT"), "1", StringComparison.Ordinal))
            EditorApplication.Exit(0);
    }
}
