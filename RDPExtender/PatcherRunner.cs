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
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
        if (string.IsNullOrWhiteSpace(systemRoot))
        {
            Console.WriteLine("WARNING: SystemRoot environment variable was not found.");
            return 1;
        }

        var termsrvDllFile = Path.Combine(systemRoot, "System32", "termsrv.dll");
        var termsrvDllCopy = Path.Combine(systemRoot, "System32", "termsrv.dll.copy");
        var termsrvPatched = Path.Combine(systemRoot, "System32", "termsrv.dll.patched");

        if (!TermServiceManager.Stop())
        {
            return 1;
        }

        FileSecurity? termsrvDllAcl = null;

        try
        {
            var termsrvFileInfo = new FileInfo(termsrvDllFile);
            termsrvDllAcl = termsrvFileInfo.GetAccessControl();

            var owner = termsrvDllAcl.GetOwner(typeof(NTAccount));
            Console.WriteLine($"Owner of termsrv.dll: {owner?.Value ?? "Unknown"}");

            if (!File.Exists(termsrvDllCopy))
            {
                File.Copy(termsrvDllFile, termsrvDllCopy, overwrite: true);
                WriteColor($"Backup created at {termsrvDllCopy}", ConsoleColor.Cyan);
            }
            else
            {
                WriteColor($"Backup already exists at {termsrvDllCopy}, skipping.", ConsoleColor.Cyan);
            }

            var takeownExitCode = ProcessRunner.Run("takeown.exe", $"/F \"{termsrvDllFile}\"");
            if (takeownExitCode != 0)
            {
                Console.WriteLine($"WARNING: takeown failed (exit code {takeownExitCode}). Cannot proceed.");
                return 1;
            }

            var currentUserName = WindowsIdentity.GetCurrent().Name;
            var icaclsExitCode = ProcessRunner.Run("icacls.exe", $"\"{termsrvDllFile}\" /grant \"{currentUserName}:F\"");
            if (icaclsExitCode != 0)
            {
                Console.WriteLine($"WARNING: icacls failed (exit code {icaclsExitCode}). Cannot proceed.");
                return 1;
            }

            var dllAsBytes = File.ReadAllBytes(termsrvDllFile);
            var dllAsText = HexConverter.BytesToHexString(dllAsBytes);
            var osInfo = OsInfoProvider.Get();

            var outcome = PatchForOperatingSystem(
                OsVersionDetector.Detect(osInfo),
                osInfo,
                dllAsText,
                termsrvDllFile,
                termsrvPatched);

            return outcome == PatchOutcome.CopyFailed ? 1 : 0;
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
                TryRestoreAcl(termsrvDllFile, termsrvDllAcl);
            }

            TermServiceManager.Start();
        }
    }

    private static PatchOutcome PatchForOperatingSystem(
        WindowsKind windowsKind,
        OsInfo osInfo,
        string dllAsText,
        string termsrvDllFile,
        string termsrvPatched)
    {
        return windowsKind switch
        {
            WindowsKind.Windows7 => PatchWindows7(osInfo, dllAsText, termsrvDllFile, termsrvPatched),
            WindowsKind.Windows10 => DllPatcher.Update(
                PatchPatterns.Standard,
                PatchPatterns.StandardReplacement,
                dllAsText,
                termsrvDllFile,
                termsrvPatched),
            WindowsKind.Windows11 => PatchWindows11(osInfo, dllAsText, termsrvDllFile, termsrvPatched),
            WindowsKind.WindowsServer2016 => PatchStandard(dllAsText, termsrvDllFile, termsrvPatched),
            WindowsKind.WindowsServer2019 => PatchStandard(dllAsText, termsrvDllFile, termsrvPatched),
            WindowsKind.WindowsServer2022 => PatchStandard(dllAsText, termsrvDllFile, termsrvPatched),
            WindowsKind.WindowsServer2025 => PatchStandard(dllAsText, termsrvDllFile, termsrvPatched),
            _ => UnsupportedOs()
        };
    }

    private static PatchOutcome PatchWindows7(
        OsInfo osInfo,
        string dllAsText,
        string termsrvDllFile,
        string termsrvPatched)
    {
        if (!Environment.Is64BitOperatingSystem)
        {
            return PatchOutcome.PatternNotFound;
        }

        return Windows7Patcher.Update(osInfo.FullOsBuild, dllAsText, termsrvDllFile, termsrvPatched);
    }

    private static PatchOutcome PatchWindows11(
        OsInfo osInfo,
        string dllAsText,
        string termsrvDllFile,
        string termsrvPatched)
    {
        if (osInfo.DisplayVersion is "23H2" or "22H2")
        {
            return PatchStandard(dllAsText, termsrvDllFile, termsrvPatched);
        }

        if (osInfo.DisplayVersion is "24H2" or "25H2")
        {
            return DllPatcher.Update(
                PatchPatterns.Win24H2,
                PatchPatterns.Win24H2Replacement,
                dllAsText,
                termsrvDllFile,
                termsrvPatched);
        }

        WriteColor($"Win11 OS Info value [{osInfo.DisplayVersion}] was not a supported value", ConsoleColor.Yellow);
        return PatchOutcome.PatternNotFound;
    }

    private static PatchOutcome PatchStandard(string dllAsText, string termsrvDllFile, string termsrvPatched)
    {
        return DllPatcher.Update(
            PatchPatterns.Standard,
            PatchPatterns.StandardReplacement,
            dllAsText,
            termsrvDllFile,
            termsrvPatched);
    }

    private static PatchOutcome UnsupportedOs()
    {
        WriteColor("Unable to get OS Version", ConsoleColor.Red);
        return PatchOutcome.PatternNotFound;
    }

    private static void TryRestoreAcl(string path, FileSecurity acl)
    {
        try
        {
            new FileInfo(path).SetAccessControl(acl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to restore ACL for {path}: {ex.Message}");
        }
    }

    private static void WriteColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
