using System;
using System.Diagnostics;

namespace RDPExtender.IO;

internal static class ProcessRunner
{
    /// <summary>
    /// Runs an executable and streams its stdout/stderr to the console.
    /// Returns the process exit code, or -1 if the process could not be started.
    /// </summary>
    public static int Run(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                Console.WriteLine($"WARNING: Failed to start '{fileName} {arguments}'.");
                return -1;
            }

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) Console.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) Console.Error.WriteLine(e.Data);
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to run '{fileName}': {ex.Message}");
            return -1;
        }
    }
}
