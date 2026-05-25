using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using RDPExtender.IO;

namespace RDPExtender.Patching;

internal static class Windows7Patcher
{
    private static readonly Regex SecondaryPattern = new(
        @"4C 24 60 BB 01 00 00 00",
        RegexOptions.Compiled);

    private static readonly Regex TertiaryPattern18 = new(
        @"83 7C 24 50 00 74 18 48 8D",
        RegexOptions.Compiled);

    private static readonly Regex TertiaryPattern43 = new(
        @"83 7C 24 50 00 74 43 48 8D",
        RegexOptions.Compiled);

    public static PatchAssessment Assess(string fullOsBuild, string termsrvDllAsText)
    {
        if (termsrvDllAsText.Contains(PatchPatterns.Win7Replacement, StringComparison.Ordinal))
        {
            return PatchAssessment.AlreadyPatched;
        }

        var dllAsTextReplaced = fullOsBuild switch
        {
            "7601.23964" => PatchBuild23964(termsrvDllAsText),
            "7601.24546" => PatchBuild24546(termsrvDllAsText),
            _ => PatchBuild24546(termsrvDllAsText)
        };

        return string.Equals(dllAsTextReplaced, termsrvDllAsText, StringComparison.Ordinal)
            ? PatchAssessment.PatternNotFound
            : PatchAssessment.NeedsPatch;
    }

    public static PatchOutcome Update(
        string fullOsBuild,
        string termsrvDllAsText,
        string termsrvDllAsFile,
        string termsrvDllAsPatch)
    {
        var dllAsTextReplaced = fullOsBuild switch
        {
            "7601.23964" => PatchBuild23964(termsrvDllAsText),
            "7601.24546" => PatchBuild24546(termsrvDllAsText),
            _ => PatchBuild24546(termsrvDllAsText)
        };

        var dllAsBytesReplaced = HexConverter.HexStringToBytes(dllAsTextReplaced);
        File.WriteAllBytes(termsrvDllAsPatch, dllAsBytesReplaced);

        ProcessRunner.Run("fc.exe", $"/b \"{termsrvDllAsPatch}\" \"{termsrvDllAsFile}\"");
        Thread.Sleep(1500);

        if (!FileCopyRetry.Copy(termsrvDllAsPatch, termsrvDllAsFile))
        {
            Console.WriteLine("WARNING: Could not replace termsrv.dll after 30 attempts. Try rebooting and running the script again.");
            return PatchOutcome.CopyFailed;
        }

        return PatchOutcome.Success;
    }

    private static string PatchBuild23964(string text)
    {
        var primaryPattern = new Regex(
            @"8B 87 38 06 00 00 39 87 3C 06 00 00 0F 84 2F C3 00 00",
            RegexOptions.Compiled);

        var replaced = primaryPattern.Replace(text, PatchPatterns.Win7Replacement, count: 1);
        replaced = SecondaryPattern.Replace(replaced, "4C 24 60 BB 00 00 00 00", count: 1);
        replaced = TertiaryPattern18.Replace(replaced, "83 7C 24 50 00 EB 18 48 8D", count: 1);
        return replaced;
    }

    private static string PatchBuild24546(string text)
    {
        var primaryPattern = new Regex(
            @"8B 87 38 06 00 00 39 87 3C 06 00 00 0F 84 3E C4 00 00",
            RegexOptions.Compiled);

        var replaced = primaryPattern.Replace(text, PatchPatterns.Win7Replacement, count: 1);
        replaced = SecondaryPattern.Replace(replaced, "4C 24 60 BB 00 00 00 00", count: 1);
        replaced = TertiaryPattern43.Replace(replaced, "83 7C 24 50 00 EB 18 48 8D", count: 1);
        return replaced;
    }
}
