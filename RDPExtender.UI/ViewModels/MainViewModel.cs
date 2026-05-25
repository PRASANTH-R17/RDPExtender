using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private readonly UiOperationLog _log;
    private bool _isBusy;
    private string _bottomBarMessage = "RDPExtender is not ready. Please fix the issues above.";
    private StatusLevel _bottomBarLevel = StatusLevel.Warning;
    private string _osStatusText = "Not Supported";
    private StatusLevel _osStatusLevel = StatusLevel.Warning;
    private string _patchStatusText = "Not Patched";
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

    public string AppVersion => "Version 1.0.0";

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
            _log.Info("Checking OS compatibility...");
            await Task.Yield();

            var snapshot = await Task.Run(RdpStatusService.GetStatus).ConfigureAwait(true);

            ApplySnapshot(snapshot);

            _log.Info($"OS: {snapshot.OsCompatibility.Text}");
            _log.Info($"Patch state: {snapshot.PatchState.Text}");
            _log.Info($"Backup: {snapshot.Backup.Text}");
            _log.Info($"RDP service: {snapshot.RdpService.Text}");

            if (snapshot.IsReady)
            {
                _log.Success("RDPExtender is ready.");
            }
            else
            {
                _log.Warning("RDPExtender is not ready. Please fix the issues above.");
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
            && PatchStatusText == "Not Patched"
            && PatchStatusLevel != StatusLevel.Error;

        CanRevert = BackupStatusLevel == StatusLevel.Ok;

        OnPropertyChanged(nameof(BottomBarIconGlyph));
    }

    private async Task PatchAsync()
    {
        IsBusy = true;
        try
        {
            _log.Info("Starting patch...");
            var result = await RdpActionService.PatchAsync(_log).ConfigureAwait(true);
            if (result.Success)
            {
                _log.Success(result.Message);
            }
            else
            {
                _log.Warning(result.Message);
                MessageBox.Show(result.Message, "Patch failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            _log.Info("Starting revert...");
            var result = await RdpActionService.RevertAsync(_log).ConfigureAwait(true);
            if (result.Success)
            {
                _log.Success(result.Message);
            }
            else
            {
                _log.Warning(result.Message);
                MessageBox.Show(result.Message, "Revert failed", MessageBoxButton.OK, MessageBoxImage.Warning);
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
}
