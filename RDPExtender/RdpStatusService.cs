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
            var error = new StatusItem("System", StatusLevel.Error, "SystemRoot not found");
            return BuildSnapshot(error, error, error, error);
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
                return new StatusItem("OS Compatibility", StatusLevel.Ok, "Supported");
            }

            if (windowsKind == WindowsKind.Windows11)
            {
                return new StatusItem("OS Compatibility", StatusLevel.Warning, "Not Supported");
            }

            return new StatusItem("OS Compatibility", StatusLevel.Warning, "Not Supported");
        }
        catch (Exception ex)
        {
            return new StatusItem("OS Compatibility", StatusLevel.Error, ex.Message);
        }
    }

    private static StatusItem EvaluatePatchState(TermsrvPaths paths, bool osSupported)
    {
        if (!osSupported)
        {
            return new StatusItem("Current Patch State", StatusLevel.Warning, "Not Patched");
        }

        try
        {
            var osInfo = OsInfoProvider.Get();
            var windowsKind = OsVersionDetector.Detect(osInfo);

            if (!PatchResolver.TryResolve(windowsKind, osInfo, out var plan, out var isWindows7, out _))
            {
                return new StatusItem("Current Patch State", StatusLevel.Warning, "Not Patched");
            }

            var dllAsBytes = File.ReadAllBytes(paths.Dll);
            var dllAsText = HexConverter.BytesToHexString(dllAsBytes);

            var assessment = isWindows7
                ? Windows7Patcher.Assess(osInfo.FullOsBuild, dllAsText)
                : DllPatcher.Assess(plan!, dllAsText);

            return assessment switch
            {
                PatchAssessment.AlreadyPatched => new StatusItem("Current Patch State", StatusLevel.Ok, "Patched"),
                PatchAssessment.NeedsPatch => new StatusItem("Current Patch State", StatusLevel.Warning, "Not Patched"),
                PatchAssessment.PatternNotFound => new StatusItem("Current Patch State", StatusLevel.Error, "Pattern Not Found"),
                _ => new StatusItem("Current Patch State", StatusLevel.Warning, "Not Patched")
            };
        }
        catch (Exception ex)
        {
            return new StatusItem("Current Patch State", StatusLevel.Error, ex.Message);
        }
    }

    private static StatusItem EvaluateBackup(TermsrvPaths paths)
    {
        return File.Exists(paths.Backup)
            ? new StatusItem("Backup Status", StatusLevel.Ok, "Available")
            : new StatusItem("Backup Status", StatusLevel.Warning, "Not Available");
    }

    private static StatusItem EvaluateRdpService()
    {
        var status = TermServiceManager.GetTermServiceStatus();
        if (status == ServiceControllerStatus.Running)
        {
            return new StatusItem("RDP Service", StatusLevel.Ok, "Running");
        }

        if (status is null)
        {
            return new StatusItem("RDP Service", StatusLevel.Error, "Unknown");
        }

        return new StatusItem("RDP Service", StatusLevel.Warning, "Stopped");
    }

    private static RdpStatusSnapshot BuildSnapshot(
        StatusItem os,
        StatusItem patch,
        StatusItem backup,
        StatusItem service)
    {
        var patchOkForReady = patch.Level == StatusLevel.Ok
            || patch.Text is "Not Patched" or "Patched";

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
            "RDPExtender is not ready. Please fix the issues above.",
            StatusLevel.Warning);
    }
}
