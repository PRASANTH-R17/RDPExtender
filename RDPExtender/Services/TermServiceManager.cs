using System;
using System.ServiceProcess;
using System.Threading;

namespace RDPExtender.Services;

internal static class TermServiceManager
{
    private const string TermService = "TermService";
    private static readonly string[] DependentServices = { "UmRdpService", "SessionEnv" };

    /// <summary>
    /// Stops dependent services first (they can hold termsrv.dll open even after
    /// TermService stops) and then stops TermService itself, polling until reported
    /// stopped. Returns false if TermService could not be stopped.
    /// </summary>
    public static bool Stop()
    {
        foreach (var name in DependentServices)
        {
            TryStop(name);
        }

        try
        {
            using var svc = new ServiceController(TermService);
            if (svc.Status != ServiceControllerStatus.Stopped)
            {
                svc.Stop();
            }

            while (true)
            {
                svc.Refresh();
                if (svc.Status == ServiceControllerStatus.Stopped)
                {
                    break;
                }
                Thread.Sleep(500);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: {ex.Message}");
            return false;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine("The Remote Desktop Services (TermService) has been stopped successfully");
        Console.WriteLine();
        Console.ResetColor();
        return true;
    }

    /// <summary>
    /// Best-effort start of TermService. Mirrors `Start-Service TermService -PassThru`.
    /// </summary>
    public static void Start()
    {
        try
        {
            using var svc = new ServiceController(TermService);
            if (svc.Status == ServiceControllerStatus.Stopped ||
                svc.Status == ServiceControllerStatus.StopPending)
            {
                svc.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to start {TermService}: {ex.Message}");
        }
    }

    private static void TryStop(string serviceName)
    {
        try
        {
            using var svc = new ServiceController(serviceName);
            if (svc.Status != ServiceControllerStatus.Stopped)
            {
                svc.Stop();
            }
        }
        catch
        {
            // Match `-ErrorAction SilentlyContinue` from the script.
        }
    }
}
