using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using RDPExtender.IO;

namespace RDPExtender;

internal static class TermsrvFileAccess
{
    public static bool GrantOwnership(string dllPath)
    {
        var takeownExitCode = ProcessRunner.Run("takeown.exe", $"/F \"{dllPath}\"");
        if (takeownExitCode != 0)
        {
            Console.WriteLine($"WARNING: takeown failed (exit code {takeownExitCode}). Cannot proceed.");
            return false;
        }

        var currentUserName = WindowsIdentity.GetCurrent().Name;
        var icaclsExitCode = ProcessRunner.Run("icacls.exe", $"\"{dllPath}\" /grant \"{currentUserName}:F\"");
        if (icaclsExitCode != 0)
        {
            Console.WriteLine($"WARNING: icacls failed (exit code {icaclsExitCode}). Cannot proceed.");
            return false;
        }

        return true;
    }

    public static void TryRestoreAcl(string path, FileSecurity acl)
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

    public static bool FilesAreIdentical(string pathA, string pathB)
    {
        var bytesA = File.ReadAllBytes(pathA);
        var bytesB = File.ReadAllBytes(pathB);
        return bytesA.AsSpan().SequenceEqual(bytesB);
    }

    public static void WriteColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
