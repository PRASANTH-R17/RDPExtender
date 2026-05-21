using System;
using Microsoft.Win32;

namespace RDPExtender.Os;

internal static class OsInfoProvider
{
    private const string CurrentVersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

    public static OsInfo Get()
    {
        using var key = Registry.LocalMachine.OpenSubKey(CurrentVersionKey)
            ?? throw new InvalidOperationException(
                $"Unable to open registry key HKLM\\{CurrentVersionKey}.");

        var currentBuild = key.GetValue("CurrentBuild")?.ToString() ?? string.Empty;
        var ubr = key.GetValue("UBR")?.ToString() ?? string.Empty;
        var displayVersion = key.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
        var installationType = key.GetValue("InstallationType")?.ToString() ?? string.Empty;

        var fullBuild = string.IsNullOrEmpty(ubr) ? currentBuild : $"{currentBuild}.{ubr}";

        return new OsInfo(currentBuild, ubr, fullBuild, displayVersion, installationType);
    }
}
