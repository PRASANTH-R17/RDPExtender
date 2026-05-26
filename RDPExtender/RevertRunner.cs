using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using RDPExtender.IO;
using RDPExtender.Services;

namespace RDPExtender;

public static class RevertRunner
{
    public static int Run()
    {
        if (!TermsrvPathResolver.TryResolve(out var paths))
        {
            Console.WriteLine("WARNING: SystemRoot environment variable was not found.");
            return 1;
        }

        if (!File.Exists(paths!.Backup))
        {
            Console.WriteLine($"WARNING: Backup not found at {paths.Backup}. Cannot revert.");
            return 1;
        }

        try
        {
            if (TermsrvFileAccess.FilesAreIdentical(paths.Dll, paths.Backup))
            {
                TermsrvFileAccess.WriteColor(
                    "The file already matches the backup. No changes are needed.",
                    ConsoleColor.Green);
                return 0;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Could not compare {paths.Dll} with backup: {ex.Message}");
            return 1;
        }

        if (!TermServiceManager.Stop())
        {
            return 1;
        }

        FileSecurity? termsrvDllAcl = null;

        try
        {
            var termsrvFileInfo = new FileInfo(paths.Dll);
            termsrvDllAcl = termsrvFileInfo.GetAccessControl();

            var owner = termsrvDllAcl.GetOwner(typeof(NTAccount));
            Console.WriteLine($"Owner of termsrv.dll: {owner?.Value ?? "Unknown"}");

            if (!TermsrvFileAccess.GrantOwnership(paths.Dll))
            {
                return 1;
            }

            if (!FileCopyRetry.Copy(paths.Backup, paths.Dll))
            {
                Console.WriteLine(
                    "WARNING: Could not replace termsrv.dll after 30 attempts. Try rebooting and running the script again.");
                return 1;
            }

            ProcessRunner.Run("fc.exe", $"/b \"{paths.Backup}\" \"{paths.Dll}\"");

            if (File.Exists(paths.Patched))
            {
                try
                {
                    File.Delete(paths.Patched);
                }
                catch
                {
                    // Non-fatal cleanup.
                }
            }

            TermsrvFileAccess.WriteColor("Restored termsrv.dll from backup.", ConsoleColor.Green);
            return 0;
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
}
