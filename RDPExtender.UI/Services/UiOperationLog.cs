using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using RDPExtender.Logging;
using RDPExtender.UI.Models;

namespace RDPExtender.UI.Services;

public sealed class UiOperationLog : IOperationLog
{
    private readonly ObservableCollection<LogEntry> _logs;
    private readonly Dispatcher _dispatcher;

    public UiOperationLog(ObservableCollection<LogEntry> logs)
    {
        _logs = logs;
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    public void Info(string message) => Append(message);

    public void Success(string message) => Append(message);

    public void Warning(string message) => Append(message);

    public void Error(string message) => Append(message);

    private void Append(string message)
    {
        var entry = new LogEntry(DateTime.Now.ToString("HH:mm:ss"), message);
        if (_dispatcher.CheckAccess())
        {
            _logs.Add(entry);
        }
        else
        {
            _dispatcher.Invoke(() => _logs.Add(entry));
        }
    }
}
