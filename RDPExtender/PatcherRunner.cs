using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using RDPExtender.IO;
using RDPExtender.Os;
using RDPExtender.Patching;
using RDPExtender.Services;

namespace RDPExtender;

internal static class PatcherRunner
{
    public static int Run()
    {
        if (!TermsrvPathResolver.TryResolve(out var paths) || paths is null)
        {
            Console.WriteLine("WARNING: SystemRoot environment variable was not found.");
            return 1;
        }

        OsInfo osInfo;
        try
        {
            osInfo = OsInfoProvider.Get();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: {ex.Message}");
            return 1;
        }

        var windowsKind = OsVersionDetector.Detect(osInfo);

        if (!PatchResolver.TryResolve(windowsKind, osInfo, out var plan, out var isWindows7, out var resolveFailure))
        {
            LogUnsupportedOs(windowsKind, osInfo, resolveFailure!.Value);
            return 1;
        }

        string dllAsText;
        try
        {
            var dllAsBytes = File.ReadAllBytes(paths.Dll);
            dllAsText = HexConverter.BytesToHexString(dllAsBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Could not read {paths.Dll}: {ex.Message}");
            return 1;
        }

        var assessment = AssessDll(isWindows7, osInfo.FullOsBuild, plan!, dllAsText);

        switch (assessment)
        {
            case PatchAssessment.UnsupportedOperatingSystem:
                TermsrvFileAccess.WriteColor("Unable to get OS Version", ConsoleColor.Red);
                return 1;

            case PatchAssessment.PatternNotFound:
                TermsrvFileAccess.WriteColor("The pattern was not found. Nothing will be changed.\n", ConsoleColor.Yellow);
                return 1;

            case PatchAssessment.AlreadyPatched:
                TermsrvFileAccess.WriteColor("The file is already patched. No changes are needed.\n", ConsoleColor.Green);
                return 0;

            case PatchAssessment.NeedsPatch:
                break;

            default:
                return 1;
        }

        if (!TermServiceManager.Stop())
        {
            return 1;
        }

        FileSecurity? termsrvDllAcl = null;
        var exitCode = 1;

        try
        {
            var termsrvFileInfo = new FileInfo(paths.Dll);
            termsrvDllAcl = termsrvFileInfo.GetAccessControl();

            var owner = termsrvDllAcl.GetOwner(typeof(NTAccount));
            Console.WriteLine($"Owner of termsrv.dll: {owner?.Value ?? "Unknown"}");

            if (!File.Exists(paths.Backup))
            {
                File.Copy(paths.Dll, paths.Backup, overwrite: true);
                TermsrvFileAccess.WriteColor($"Backup created at {paths.Backup}", ConsoleColor.Cyan);
            }
            else
            {
                TermsrvFileAccess.WriteColor($"Backup already exists at {paths.Backup}, skipping.", ConsoleColor.Cyan);
            }

            if (!TermsrvFileAccess.GrantOwnership(paths.Dll))
            {
                return 1;
            }

            string dllAsTextAfterAcl;
            try
            {
                var dllAsBytes = File.ReadAllBytes(paths.Dll);
                dllAsTextAfterAcl = HexConverter.BytesToHexString(dllAsBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Could not re-read {paths.Dll}: {ex.Message}");
                return 1;
            }

            var reassess = AssessDll(isWindows7, osInfo.FullOsBuild, plan!, dllAsTextAfterAcl);
            if (reassess != PatchAssessment.NeedsPatch)
            {
                exitCode = reassess == PatchAssessment.AlreadyPatched ? 0 : 1;
                return exitCode;
            }

            var outcome = ApplyPatch(isWindows7, osInfo.FullOsBuild, plan!, dllAsTextAfterAcl, paths.Dll, paths.Patched);
            exitCode = MapPatchOutcomeToExitCode(outcome);
            return exitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: {ex.Message}");
            return 1;
        }
        finally
        {
            if (termsrvDllAcl is not null)
            {
                TermsrvFileAccess.TryRestoreAcl(paths.Dll, termsrvDllAcl);
            }

            TermServiceManager.Start();
        }
    }

    private static PatchAssessment AssessDll(bool isWindows7, string fullOsBuild, PatchPlan plan, string dllAsText)
    {
        return isWindows7
            ? Windows7Patcher.Assess(fullOsBuild, dllAsText)
            : DllPatcher.Assess(plan, dllAsText);
    }

    private static PatchOutcome ApplyPatch(
        bool isWindows7,
        string fullOsBuild,
        PatchPlan plan,
        string dllAsText,
        string termsrvDllFile,
        string termsrvPatched)
    {
        return isWindows7
            ? Windows7Patcher.Update(fullOsBuild, dllAsText, termsrvDllFile, termsrvPatched)
            : DllPatcher.Update(plan, dllAsText, termsrvDllFile, termsrvPatched);
    }

    private static int MapPatchOutcomeToExitCode(PatchOutcome outcome)
    {
        return outcome switch
        {
            PatchOutcome.CopyFailed => 1,
            PatchOutcome.PatternNotFound => 1,
            _ => 0
        };
    }

    private static void LogUnsupportedOs(WindowsKind windowsKind, OsInfo osInfo, PatchAssessment failure)
    {
        if (windowsKind == WindowsKind.Windows11 &&
            failure == PatchAssessment.UnsupportedOperatingSystem)
        {
            TermsrvFileAccess.WriteColor(
                $"Win11 OS Info value [{osInfo.DisplayVersion}] was not a supported value",
                ConsoleColor.Yellow);
            return;
        }

        if (windowsKind == WindowsKind.Windows7 &&
            !Environment.Is64BitOperatingSystem)
        {
            TermsrvFileAccess.WriteColor("Windows 7 32-bit is not supported.", ConsoleColor.Red);
            return;
        }

        TermsrvFileAccess.WriteColor("Unable to get OS Version", ConsoleColor.Red);
    }
}
