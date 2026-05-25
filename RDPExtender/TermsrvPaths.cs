using System;
using System.IO;

namespace RDPExtender;

internal sealed record TermsrvPaths(string Dll, string Backup, string Patched);

internal static class TermsrvPathResolver
{
    public static bool TryResolve(out TermsrvPaths? paths)
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
        if (string.IsNullOrWhiteSpace(systemRoot))
        {
            paths = null;
            return false;
        }

        paths = new TermsrvPaths(
            Path.Combine(systemRoot, "System32", "termsrv.dll"),
            Path.Combine(systemRoot, "System32", "termsrv.dll.copy"),
            Path.Combine(systemRoot, "System32", "termsrv.dll.patched"));
        return true;
    }
}
