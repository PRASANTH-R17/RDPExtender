using System;
using System.IO;
using System.Threading;

namespace RDPExtender.IO;

internal static class FileCopyRetry
{
    private const int MaxAttempts = 30;
    private const int DelayMilliseconds = 2000;

    /// <summary>
    /// Retries Copy up to 30 times with a 2-second delay to handle the window where
    /// dependent services have stopped but still hold a file lock on termsrv.dll.
    /// </summary>
    public static bool Copy(string source, string destination)
    {
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                File.Copy(source, destination, overwrite: true);
                return true;
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Waiting for DLL lock to release (attempt {attempt}/{MaxAttempts})...");
                Console.ResetColor();
                Thread.Sleep(DelayMilliseconds);
            }
        }
        return false;
    }
}
