using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using RDPExtender;
using RDPExtender.Models;
using RDPExtender.UI.Models;
using RDPExtender.UI.Services;

namespace RDPExtender.UI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private const string MultipleUserAccessNotEnabled = "Not Enabled";
    private readonly UiOperationLog _log;
    private readonly string _appVersion = BuildAppVersion();
    private bool _isBusy;
    private string _bottomBarMessage = "RDPExtender needs attention. Please check the status above.";
    private StatusLevel _bottomBarLevel = StatusLevel.Warning;
    private string _osStatusText = "Not Supported";
    private StatusLevel _osStatusLevel = StatusLevel.Warning;
    private string _patchStatusText = MultipleUserAccessNotEnabled;
    private StatusLevel _patchStatusLevel = StatusLevel.Warning;
    private string _backupStatusText = "Not Available";
    private StatusLevel _backupStatusLevel = StatusLevel.Warning;
    private string _serviceStatusText = "Stopped";
    private StatusLevel _serviceStatusLevel = StatusLevel.Warning;
    private bool _canPatch;
    private bool _canRevert;

    public MainViewModel()
    {
        Logs = new ObservableCollection<LogEntry>();
        _log = new UiOperationLog(Logs);

        RefreshStatusCommand = new RelayCommand(RefreshStatusAsync, () => !IsBusy);
        PatchCommand = new RelayCommand(PatchAsync, () => !IsBusy && CanPatch);
        RevertCommand = new RelayCommand(RevertAsync, () => !IsBusy && CanRevert);
        ClearLogsCommand = new RelayCommand(ClearLogsAsync, () => !IsBusy);
    }

    public ObservableCollection<LogEntry> Logs { get; }

    public string AppVersion => _appVersion;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string BottomBarMessage
    {
        get => _bottomBarMessage;
        private set => SetProperty(ref _bottomBarMessage, value);
    }

    public StatusLevel BottomBarLevel
    {
        get => _bottomBarLevel;
        private set => SetProperty(ref _bottomBarLevel, value);
    }

    public string BottomBarIconGlyph => BottomBarLevel switch
    {
        StatusLevel.Ok => "\uEA18",
        StatusLevel.Error => "\uE783",
        _ => "\uE7BA"
    };

    public string OsStatusText
    {
        get => _osStatusText;
        private set => SetProperty(ref _osStatusText, value);
    }

    public StatusLevel OsStatusLevel
    {
        get => _osStatusLevel;
        private set => SetProperty(ref _osStatusLevel, value);
    }

    public string PatchStatusText
    {
        get => _patchStatusText;
        private set => SetProperty(ref _patchStatusText, value);
    }

    public StatusLevel PatchStatusLevel
    {
        get => _patchStatusLevel;
        private set => SetProperty(ref _patchStatusLevel, value);
    }

    public string BackupStatusText
    {
        get => _backupStatusText;
        private set => SetProperty(ref _backupStatusText, value);
    }

    public StatusLevel BackupStatusLevel
    {
        get => _backupStatusLevel;
        private set => SetProperty(ref _backupStatusLevel, value);
    }

    public string ServiceStatusText
    {
        get => _serviceStatusText;
        private set => SetProperty(ref _serviceStatusText, value);
    }

    public StatusLevel ServiceStatusLevel
    {
        get => _serviceStatusLevel;
        private set => SetProperty(ref _serviceStatusLevel, value);
    }

    public bool CanPatch
    {
        get => _canPatch;
        private set
        {
            if (SetProperty(ref _canPatch, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public bool CanRevert
    {
        get => _canRevert;
        private set
        {
            if (SetProperty(ref _canRevert, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public ICommand RefreshStatusCommand { get; }

    public ICommand PatchCommand { get; }

    public ICommand RevertCommand { get; }

    public ICommand ClearLogsCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task RefreshStatusAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Yield();

            var snapshot = await Task.Run(RdpStatusService.GetStatus).ConfigureAwait(true);

            ApplySnapshot(snapshot);

            _log.Info("Status refreshed.");

            if (snapshot.IsReady)
            {
                _log.Success("RDPExtender is ready.");
            }
            else
            {
                _log.Warning("RDPExtender needs attention. Please check the status above.");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySnapshot(RdpStatusSnapshot snapshot)
    {
        OsStatusText = snapshot.OsCompatibility.Text;
        OsStatusLevel = snapshot.OsCompatibility.Level;
        PatchStatusText = snapshot.PatchState.Text;
        PatchStatusLevel = snapshot.PatchState.Level;
        BackupStatusText = snapshot.Backup.Text;
        BackupStatusLevel = snapshot.Backup.Level;
        ServiceStatusText = snapshot.RdpService.Text;
        ServiceStatusLevel = snapshot.RdpService.Level;
        BottomBarMessage = snapshot.BottomBarMessage;
        BottomBarLevel = snapshot.BottomBarLevel;

        CanPatch = OsStatusLevel == StatusLevel.Ok
            && PatchStatusText == MultipleUserAccessNotEnabled
            && PatchStatusLevel == StatusLevel.Warning;

        CanRevert = BackupStatusLevel == StatusLevel.Ok;

        OnPropertyChanged(nameof(BottomBarIconGlyph));
    }

    private async Task PatchAsync()
    {
        IsBusy = true;
        try
        {
            _log.Info("Enabling multiple user access...");
            var result = await RdpActionService.PatchAsync(_log).ConfigureAwait(true);
            if (result.Success)
            {
                _log.Success(result.Message);
            }
            else
            {
                _log.Warning(result.Message);
                MessageBox.Show(result.Message, "Enable action failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            IsBusy = false;
            await RefreshStatusAsync();
        }
    }

    private async Task RevertAsync()
    {
        IsBusy = true;
        try
        {
            _log.Info("Restoring original settings...");
            var result = await RdpActionService.RevertAsync(_log).ConfigureAwait(true);
            if (result.Success)
            {
                _log.Success(result.Message);
            }
            else
            {
                _log.Warning(result.Message);
                MessageBox.Show(result.Message, "Restore failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        finally
        {
            IsBusy = false;
            await RefreshStatusAsync();
        }
    }

    private Task ClearLogsAsync()
    {
        Logs.Clear();
        return Task.CompletedTask;
    }

    private void RaiseCommandStates()
    {
        (RefreshStatusCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PatchCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RevertCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ClearLogsCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static string BuildAppVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var cleanInformational = informational.Split('+')[0];
            return $"Version {cleanInformational}";
        }

        var version = assembly.GetName().Version;
        if (version is null)
        {
            return "Version 1.0.0";
        }

        return version.Build >= 0
            ? $"Version {version.Major}.{version.Minor}.{version.Build}"
            : $"Version {version.Major}.{version.Minor}.0";
    }
}
