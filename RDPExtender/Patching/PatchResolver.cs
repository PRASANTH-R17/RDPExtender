using System;
using RDPExtender.Os;

namespace RDPExtender.Patching;

internal static class PatchResolver
{
    /// <summary>
    /// Resolves the patch plan for the detected OS without reading termsrv.dll.
    /// </summary>
    /// <param name="plan">Set when resolve succeeds with a standard patch plan.</param>
    /// <param name="isWindows7">True when Win7 patching should use <see cref="Windows7Patcher"/>.</param>
    /// <param name="failure">Set when the OS or configuration is not supported.</param>
    /// <returns>True when patching may proceed (plan or Win7 path); false when unsupported.</returns>
    public static bool TryResolve(
        WindowsKind windowsKind,
        OsInfo osInfo,
        out PatchPlan? plan,
        out bool isWindows7,
        out PatchAssessment? failure)
    {
        plan = null;
        isWindows7 = false;
        failure = null;

        switch (windowsKind)
        {
            case WindowsKind.Unsupported:
                failure = PatchAssessment.UnsupportedOperatingSystem;
                return false;

            case WindowsKind.Windows7:
                if (!Environment.Is64BitOperatingSystem)
                {
                    failure = PatchAssessment.UnsupportedOperatingSystem;
                    return false;
                }

                isWindows7 = true;
                return true;

            case WindowsKind.Windows10:
            case WindowsKind.WindowsServer2016:
            case WindowsKind.WindowsServer2019:
            case WindowsKind.WindowsServer2022:
            case WindowsKind.WindowsServer2025:
                plan = new PatchPlan(PatchPatterns.Standard, PatchPatterns.StandardReplacement);
                return true;

            case WindowsKind.Windows11:
                if (osInfo.DisplayVersion is "23H2" or "22H2")
                {
                    plan = new PatchPlan(PatchPatterns.Standard, PatchPatterns.StandardReplacement);
                    return true;
                }

                if (osInfo.DisplayVersion is "24H2" or "25H2")
                {
                    plan = new PatchPlan(PatchPatterns.Win24H2, PatchPatterns.Win24H2Replacement);
                    return true;
                }

                failure = PatchAssessment.UnsupportedOperatingSystem;
                return false;

            default:
                failure = PatchAssessment.UnsupportedOperatingSystem;
                return false;
        }
    }
}
