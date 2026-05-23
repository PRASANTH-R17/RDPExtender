using System;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using RDPExtender.IO;

namespace RDPExtender.Patching;

internal enum PatchOutcome
{
    Success,
    AlreadyPatched,
    PatternNotFound,
    CopyFailed
}

internal static class DllPatcher
{
    public static PatchAssessment Assess(PatchPlan plan, string termsrvDllAsText)
    {
        if (plan.Pattern.IsMatch(termsrvDllAsText))
        {
            return PatchAssessment.NeedsPatch;
        }

        if (termsrvDllAsText.Contains(plan.Replacement, StringComparison.Ordinal))
        {
            return PatchAssessment.AlreadyPatched;
        }

        return PatchAssessment.PatternNotFound;
    }

    public static PatchOutcome Update(
        PatchPlan plan,
        string termsrvDllAsText,
        string termsrvDllAsFile,
        string termsrvDllAsPatch)
    {
        WriteColor("\nPattern matching!\n", ConsoleColor.Green);

        // Replace only the first occurrence to avoid corrupting the DLL if the
        // byte sequence appears more than once.
        var dllAsTextReplaced = plan.Pattern.Replace(termsrvDllAsText, plan.Replacement, count: 1);
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

    private static void WriteColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
