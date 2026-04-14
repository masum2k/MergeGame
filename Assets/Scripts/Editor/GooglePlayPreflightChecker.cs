#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class GooglePlayPreflightChecker
{
    [MenuItem("Tools/Release/Google Play Preflight Report")]
    public static void RunReport()
    {
        List<string> failures = new List<string>();
        List<string> warnings = new List<string>();
        List<string> passes = new List<string>();

        NamedBuildTarget androidTarget = NamedBuildTarget.Android;

        string packageName = PlayerSettings.GetApplicationIdentifier(androidTarget);
        if (string.IsNullOrWhiteSpace(packageName) || packageName.StartsWith("com.DefaultCompany") || !IsValidPackageName(packageName))
        {
            failures.Add("Android package name invalid/default. Set a unique package (example: com.company.game).\nCurrent: " + packageName);
        }
        else
        {
            passes.Add("Package name OK: " + packageName);
        }

        if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) || PlayerSettings.companyName == "DefaultCompany")
        {
            failures.Add("Company Name is still default. Update it in Player Settings.");
        }
        else
        {
            passes.Add("Company Name set: " + PlayerSettings.companyName);
        }

        if (string.IsNullOrWhiteSpace(PlayerSettings.productName) || PlayerSettings.productName == "New Unity Project")
        {
            warnings.Add("Product Name looks generic. Verify final store-facing name.");
        }
        else
        {
            passes.Add("Product Name set: " + PlayerSettings.productName);
        }

        if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion))
        {
            failures.Add("Bundle Version is empty.");
        }
        else
        {
            passes.Add("Bundle Version: " + PlayerSettings.bundleVersion);
        }

        if (PlayerSettings.Android.bundleVersionCode <= 0)
        {
            failures.Add("Android Version Code must be > 0.");
        }
        else
        {
            passes.Add("Android Version Code: " + PlayerSettings.Android.bundleVersionCode);
        }

        int minSdk = (int)PlayerSettings.Android.minSdkVersion;
        if (minSdk < 24)
        {
            failures.Add("Min SDK is below 24. Increase to API 24+ for modern Android support.");
        }
        else
        {
            passes.Add("Min SDK OK: API " + minSdk);
        }

        if (PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto)
        {
            warnings.Add("Target SDK is Auto. This is acceptable, but verify Play requirement at release time.");
        }
        else
        {
            passes.Add("Target SDK explicitly set: API " + (int)PlayerSettings.Android.targetSdkVersion);
        }

        AndroidArchitecture arch = PlayerSettings.Android.targetArchitectures;
        if ((arch & AndroidArchitecture.ARM64) == 0)
        {
            failures.Add("ARM64 is not enabled. Google Play requires 64-bit support.");
        }
        else
        {
            passes.Add("ARM64 enabled.");
        }

        if (!EditorUserBuildSettings.buildAppBundle)
        {
            warnings.Add("Build App Bundle is disabled. Play uploads should use .aab.");
        }
        else
        {
            passes.Add("App Bundle build enabled.");
        }

        bool? minifyReleaseEnabled = TryGetMinifyRelease(androidTarget);
        if (!minifyReleaseEnabled.HasValue)
        {
            warnings.Add("Release minify status could not be resolved for this Unity version. Verify manually in Player Settings > Android > Publishing Settings.");
        }
        else if (!minifyReleaseEnabled.Value)
        {
            warnings.Add("Minify Release is disabled. Consider enabling for smaller APK/AAB size.");
        }
        else
        {
            passes.Add("Minify Release enabled.");
        }

        ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(androidTarget);
        if (backend != ScriptingImplementation.IL2CPP)
        {
            failures.Add("Scripting backend is not IL2CPP. Set Android backend to IL2CPP for release.");
        }
        else
        {
            passes.Add("IL2CPP backend enabled.");
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            warnings.Add("Active build target is not Android.");
        }
        else
        {
            passes.Add("Active build target is Android.");
        }

        string report = BuildReportText(failures, warnings, passes);

        if (failures.Count > 0)
        {
            Debug.LogError(report);
        }
        else if (warnings.Count > 0)
        {
            Debug.LogWarning(report);
        }
        else
        {
            Debug.Log(report);
        }

        EditorUtility.DisplayDialog(
            "Google Play Preflight",
            "Failures: " + failures.Count + "\nWarnings: " + warnings.Count + "\nPass: " + passes.Count +
            "\n\nFull report logged to Console.",
            "OK");
    }

    private static bool? TryGetMinifyRelease(NamedBuildTarget buildTarget)
    {
        // Newer API path
        MethodInfo typedMethod = typeof(PlayerSettings).GetMethod(
            "GetMinifyRelease",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(NamedBuildTarget) },
            null);

        if (typedMethod != null)
        {
            object value = typedMethod.Invoke(null, new object[] { buildTarget });
            if (value is bool asBool)
                return asBool;
        }

        // Legacy fallback path
        MethodInfo legacyPropertyMethod = typeof(PlayerSettings).GetMethod(
            "GetPropertyOptionalBool",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string), typeof(BuildTargetGroup), typeof(bool) },
            null);

        if (legacyPropertyMethod != null)
        {
            object value = legacyPropertyMethod.Invoke(null, new object[] { "AndroidMinifyRelease", BuildTargetGroup.Android, false });
            if (value is bool asBool)
                return asBool;
        }

        PropertyInfo directProperty = typeof(PlayerSettings).GetProperty("AndroidMinifyRelease", BindingFlags.Public | BindingFlags.Static);
        if (directProperty != null && directProperty.PropertyType == typeof(bool))
        {
            object value = directProperty.GetValue(null, null);
            if (value is bool asBool)
                return asBool;
        }

        return null;
    }

    private static bool IsValidPackageName(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;

        // Lowercase package style: com.company or com.company.product
        return Regex.IsMatch(packageName, "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*){1,}$");
    }

    private static string BuildReportText(List<string> failures, List<string> warnings, List<string> passes)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Google Play Preflight Report ===");
        sb.AppendLine("Project: " + PlayerSettings.productName);
        sb.AppendLine();

        sb.AppendLine("[FAILURES]");
        if (failures.Count == 0)
        {
            sb.AppendLine("- None");
        }
        else
        {
            for (int i = 0; i < failures.Count; i++)
            {
                sb.AppendLine("- " + failures[i]);
            }
        }

        sb.AppendLine();
        sb.AppendLine("[WARNINGS]");
        if (warnings.Count == 0)
        {
            sb.AppendLine("- None");
        }
        else
        {
            for (int i = 0; i < warnings.Count; i++)
            {
                sb.AppendLine("- " + warnings[i]);
            }
        }

        sb.AppendLine();
        sb.AppendLine("[PASS]");
        if (passes.Count == 0)
        {
            sb.AppendLine("- None");
        }
        else
        {
            for (int i = 0; i < passes.Count; i++)
            {
                sb.AppendLine("- " + passes[i]);
            }
        }

        return sb.ToString();
    }
}
#endif