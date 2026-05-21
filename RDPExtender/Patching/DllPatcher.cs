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
    public static PatchOutcome Update(
        Regex inputPattern,
        string replacement,
        string termsrvDllAsText,
        string termsrvDllAsFile,
        string termsrvDllAsPatch)
    {
        var match = inputPattern.IsMatch(termsrvDllAsText);
        var alreadyPatched = termsrvDllAsText.Contains(replacement, StringComparison.Ordinal);

        if (match)
        {
            WriteColor("\nPattern matching!\n", ConsoleColor.Green);

            // Replace only the first occurrence to avoid corrupting the DLL if the
            // byte sequence appears more than once.
            var dllAsTextReplaced = inputPattern.Replace(termsrvDllAsText, replacement, count: 1);
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

        if (alreadyPatched)
        {
            WriteColor("The file is already patched. No changes are needed.\n", ConsoleColor.Green);
            return PatchOutcome.AlreadyPatched;
        }

        WriteColor("The pattern was not found. Nothing will be changed.\n", ConsoleColor.Yellow);
        return PatchOutcome.PatternNotFound;
    }

    private static void WriteColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
