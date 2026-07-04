using System.Text.RegularExpressions;

namespace RDPExtender.Patching;

internal static class PatchPatterns
{
    /// <summary>
    /// Default pattern used by Windows 10, Windows 11 22H2/23H2, and Windows Server
    /// 2016/2019/2022/2025.
    /// </summary>
    public static readonly Regex Standard = new(
        @"39 81 3C 06 00 00 0F (?:[0-9A-F]{2} ){4}00",
        RegexOptions.Compiled);

    /// <summary>
    /// Pattern used by early Windows 11 24H2/25H2 builds.
    /// </summary>
    public static readonly Regex Win24H2 = new(
        @"8B 81 38 06 00 00 39 81 3C 06 00 00 75",
        RegexOptions.Compiled);

    /// <summary>
    /// Pattern used by newer Windows 11 24H2/25H2 cumulative updates (e.g. termsrv 26100.8737).
    /// </summary>
    public static readonly Regex Win25H2 = new(
        @"44 8B 87 3C 06 00 00 44 8B 8F 38 06 00 00 45 3B C1 75 14",
        RegexOptions.Compiled);

    /// <summary>
    /// Standard replacement for all non-Win7, non-Win11-24H2/25H2 branches.
    /// </summary>
    public const string StandardReplacement = "B8 00 01 00 00 89 81 38 06 00 00 90";

    /// <summary>
    /// Replacement for early Windows 11 24H2/25H2 builds (adds trailing EB).
    /// </summary>
    public const string Win24H2Replacement = "B8 00 01 00 00 89 81 38 06 00 00 90 EB";

    /// <summary>
    /// Replacement for newer Windows 11 24H2/25H2 cumulative updates.
    /// </summary>
    public const string Win25H2Replacement =
        "41 B9 00 01 00 00 90 44 89 8F 38 06 00 00 90 90 90 EB 14";

    /// <summary>
    /// Replacement for Windows 7 64-bit primary pattern.
    /// </summary>
    public const string Win7Replacement =
        "B8 00 01 00 00 90 89 87 38 06 00 00 90 90 90 90 90 90";
}
