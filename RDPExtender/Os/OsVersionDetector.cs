using System;

namespace RDPExtender.Os;

internal enum WindowsKind
{
    Windows7,
    Windows10,
    Windows11,
    WindowsServer2016,
    WindowsServer2019,
    WindowsServer2022,
    WindowsServer2025,
    Unsupported
}

internal static class OsVersionDetector
{
    public static WindowsKind Detect(OsInfo info)
    {
        var version = Environment.OSVersion.Version;

        if (string.Equals(info.InstallationType, "Client", StringComparison.OrdinalIgnoreCase))
        {
            if (version.Major == 6 && version.Minor == 1)
            {
                return WindowsKind.Windows7;
            }
            if (version.Major == 10 && version.Build < 22000)
            {
                return WindowsKind.Windows10;
            }
            if (version.Major == 10 && version.Build >= 22000)
            {
                return WindowsKind.Windows11;
            }
            return WindowsKind.Unsupported;
        }

        if (string.Equals(info.InstallationType, "Server", StringComparison.OrdinalIgnoreCase))
        {
            // Explicit build numbers - Server 2022 (20348) is < 22000, so a -lt 22000
            // range check would incorrectly classify it as Server 2016.
            if (version.Major == 10 && version.Build == 14393)
            {
                return WindowsKind.WindowsServer2016;
            }
            if (version.Major == 10 && version.Build == 17763)
            {
                return WindowsKind.WindowsServer2019;
            }
            if (version.Major == 10 && (version.Build == 20348 || version.Build == 25398))
            {
                return WindowsKind.WindowsServer2022;
            }
            if (version.Major == 10 && version.Build >= 26100)
            {
                return WindowsKind.WindowsServer2025;
            }
            return WindowsKind.Unsupported;
        }

        return WindowsKind.Unsupported;
    }
}
