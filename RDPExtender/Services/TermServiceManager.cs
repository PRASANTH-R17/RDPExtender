using System;
using System.ServiceProcess;
using System.Threading;

namespace RDPExtender.Services;

internal static class TermServiceManager
{
    private const string TermService = "TermService";
    private static readonly string[] DependentServices = { "UmRdpService", "SessionEnv" };

    /// <summary>
    /// Returns the current status of TermService, or null if it cannot be queried
    /// (e.g. service not installed or access denied). If the service is in a
    /// transient state (StartPending / StopPending / ...), waits briefly for it
    /// to settle so callers don't observe flicker.
    /// </summary>
    public static ServiceControllerStatus? GetTermServiceStatus()
    {
        try
        {
            using var svc = new ServiceController(TermService);
            var status = svc.Status;

            if (IsTransient(status))
            {
                var target = status == ServiceControllerStatus.StopPending
                    ? ServiceControllerStatus.Stopped
                    : ServiceControllerStatus.Running;

                try
                {
                    svc.WaitForStatus(target, TimeSpan.FromSeconds(5));
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    // Fall through — return whatever the service settled at.
                }

                svc.Refresh();
                status = svc.Status;
            }

            return status;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsTransient(ServiceControllerStatus status) =>
        status == ServiceControllerStatus.StartPending
        || status == ServiceControllerStatus.StopPending
        || status == ServiceControllerStatus.ContinuePending
        || status == ServiceControllerStatus.PausePending;

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
    /// Best-effort start of TermService. Waits up to 15 seconds for the service
    /// to reach the Running state so a subsequent status read isn't racy.
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

            try
            {
                svc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
            }
            catch (System.ServiceProcess.TimeoutException)
            {
                Console.WriteLine($"WARNING: {TermService} did not reach Running state within 15 seconds.");
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

            if (svc.Status == ServiceControllerStatus.Stopped)
                return;

            if (svc.CanStop)
            {
                svc.Stop();
            }

            svc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to stop {serviceName}: {ex.Message}");
        }
    }
}
