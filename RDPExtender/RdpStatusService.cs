using System.IO;
using System.ServiceProcess;
using RDPExtender.Models;
using RDPExtender.Os;
using RDPExtender.Patching;
using RDPExtender.Services;

namespace RDPExtender;

public static class RdpStatusService
{
    public static RdpStatusSnapshot GetStatus()
    {
        if (!TermsrvPathResolver.TryResolve(out var paths) || paths is null)
        {
            var os = new StatusItem("Windows Compatibility", StatusLevel.Warning, "Not Supported");
            var access = new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled");
            var restore = new StatusItem("Restore Point", StatusLevel.Warning, "Not Available");
            var service = new StatusItem("Remote Desktop Service", StatusLevel.Warning, "Stopped");
            return BuildSnapshot(os, access, restore, service);
        }

        var osItem = EvaluateOsCompatibility();
        var patchItem = EvaluatePatchState(paths, osItem.Level == StatusLevel.Ok);
        var backupItem = EvaluateBackup(paths);
        var serviceItem = EvaluateRdpService();

        return BuildSnapshot(osItem, patchItem, backupItem, serviceItem);
    }

    private static StatusItem EvaluateOsCompatibility()
    {
        try
        {
            var osInfo = OsInfoProvider.Get();
            var windowsKind = OsVersionDetector.Detect(osInfo);

            if (PatchResolver.TryResolve(windowsKind, osInfo, out _, out _, out _))
            {
                return new StatusItem("Windows Compatibility", StatusLevel.Ok, "Supported");
            }

            if (windowsKind == WindowsKind.Windows11)
            {
                return new StatusItem("Windows Compatibility", StatusLevel.Warning, "Not Supported");
            }

            return new StatusItem("Windows Compatibility", StatusLevel.Warning, "Not Supported");
        }
        catch (Exception)
        {
            return new StatusItem("Windows Compatibility", StatusLevel.Warning, "Not Supported");
        }
    }

    private static StatusItem EvaluatePatchState(TermsrvPaths paths, bool osSupported)
    {
        if (!osSupported)
        {
            return new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled");
        }

        try
        {
            var osInfo = OsInfoProvider.Get();
            var windowsKind = OsVersionDetector.Detect(osInfo);

            if (!PatchResolver.TryResolve(windowsKind, osInfo, out var plans, out var isWindows7, out _))
            {
                return new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled");
            }

            var dllAsBytes = File.ReadAllBytes(paths.Dll);
            var dllAsText = HexConverter.BytesToHexString(dllAsBytes);

            var assessment = isWindows7
                ? Windows7Patcher.Assess(osInfo.FullOsBuild, dllAsText)
                : DllPatcher.Assess(plans!, dllAsText);

            return assessment switch
            {
                PatchAssessment.AlreadyPatched => new StatusItem("Multiple User Access", StatusLevel.Ok, "Enabled"),
                PatchAssessment.NeedsPatch => new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled"),
                PatchAssessment.PatternNotFound => new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled"),
                _ => new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled")
            };
        }
        catch (Exception)
        {
            return new StatusItem("Multiple User Access", StatusLevel.Warning, "Not Enabled");
        }
    }

    private static StatusItem EvaluateBackup(TermsrvPaths paths)
    {
        return File.Exists(paths.Backup)
            ? new StatusItem("Restore Point", StatusLevel.Ok, "Available")
            : new StatusItem("Restore Point", StatusLevel.Warning, "Not Available");
    }

    private static StatusItem EvaluateRdpService()
    {
        var status = TermServiceManager.GetTermServiceStatus();
        if (status == ServiceControllerStatus.Running)
        {
            return new StatusItem("Remote Desktop Service", StatusLevel.Ok, "Running");
        }

        if (status is null)
        {
            return new StatusItem("Remote Desktop Service", StatusLevel.Warning, "Stopped");
        }

        return new StatusItem("Remote Desktop Service", StatusLevel.Warning, "Stopped");
    }

    private static RdpStatusSnapshot BuildSnapshot(
        StatusItem os,
        StatusItem patch,
        StatusItem backup,
        StatusItem service)
    {
        var patchOkForReady = patch.Level == StatusLevel.Ok
            || patch.Text is "Not Enabled" or "Enabled";

        var isReady = os.Level == StatusLevel.Ok
            && patchOkForReady
            && patch.Level != StatusLevel.Error
            && backup.Level == StatusLevel.Ok
            && service.Level == StatusLevel.Ok;

        if (isReady)
        {
            return new RdpStatusSnapshot(
                os, patch, backup, service,
                true,
                "RDPExtender is ready.",
                StatusLevel.Ok);
        }

        return new RdpStatusSnapshot(
            os, patch, backup, service,
            false,
            "RDPExtender needs attention. Please check the status above.",
            StatusLevel.Warning);
    }
}
