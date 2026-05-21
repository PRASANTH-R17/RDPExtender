using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;

namespace RDPExtender;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (!IsAdministrator())
        {
            return TryElevate(args);
        }

        return PatcherRunner.Run();
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static int TryElevate(string[] args)
    {
        var message = CultureInfo.CurrentCulture.Name == "pt-BR"
            ? "Você não executou este script como Administrador. Este script será executado automaticamente como Administrador."
            : "You didn't run this script as an Administrator. This script will self elevate to run as an Administrator and continue.";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();

        Thread.Sleep(2500);

        var commandLineArgs = Environment.GetCommandLineArgs();
        var executablePath = Environment.ProcessPath ?? commandLineArgs[0];
        var relaunchArgs = IsDotNetHost(executablePath)
            ? commandLineArgs.Take(1).Concat(args)
            : args;
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = string.Join(" ", relaunchArgs.Select(QuoteArgument)),
            Verb = "runas",
            UseShellExecute = true
        };

        try
        {
            Process.Start(startInfo);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to self elevate: {ex.Message}");
            return 1;
        }
    }

    private static string QuoteArgument(string argument)
    {
        return argument.Contains(' ', StringComparison.Ordinal) || argument.Contains('"', StringComparison.Ordinal)
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;
    }

    private static bool IsDotNetHost(string executablePath)
    {
        return string.Equals(Path.GetFileName(executablePath), "dotnet.exe", StringComparison.OrdinalIgnoreCase);
    }
}
