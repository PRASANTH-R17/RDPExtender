using System;
using System.Collections.Generic;
using RDPExtender.Os;

namespace RDPExtender.Patching;

internal static class PatchResolver
{
    private static readonly PatchPlan StandardPlan =
        new(PatchPatterns.Standard, PatchPatterns.StandardReplacement);

    private static readonly PatchPlan Win24H2Plan =
        new(PatchPatterns.Win24H2, PatchPatterns.Win24H2Replacement);

    private static readonly PatchPlan Win25H2Plan =
        new(PatchPatterns.Win25H2, PatchPatterns.Win25H2Replacement);

    private static readonly IReadOnlyList<PatchPlan> Win11ModernPlans =
        [Win24H2Plan, Win25H2Plan];

    /// <summary>
    /// Resolves the patch plan(s) for the detected OS without reading termsrv.dll.
    /// </summary>
    /// <param name="plans">Set when resolve succeeds with one or more candidate patch plans.</param>
    /// <param name="isWindows7">True when Win7 patching should use <see cref="Windows7Patcher"/>.</param>
    /// <param name="failure">Set when the OS or configuration is not supported.</param>
    /// <returns>True when patching may proceed (plans or Win7 path); false when unsupported.</returns>
    public static bool TryResolve(
        WindowsKind windowsKind,
        OsInfo osInfo,
        out IReadOnlyList<PatchPlan>? plans,
        out bool isWindows7,
        out PatchAssessment? failure)
    {
        plans = null;
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
                plans = [StandardPlan];
                return true;

            case WindowsKind.Windows11:
                if (osInfo.DisplayVersion is "23H2" or "22H2")
                {
                    plans = [StandardPlan];
                    return true;
                }

                if (osInfo.DisplayVersion is "24H2" or "25H2")
                {
                    // Recent cumulative updates can ship either byte sequence on 24H2/25H2.
                    plans = Win11ModernPlans;
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
