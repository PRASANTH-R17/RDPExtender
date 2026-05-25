using System.Collections.Specialized;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using RDPExtender.UI.Models;
using RDPExtender.UI.ViewModels;

namespace RDPExtender.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Brush _timestampBrush;
    private readonly Brush _messageBrush;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        _timestampBrush = (Brush)(TryFindResource("PrimaryBlueBrush") ?? Brushes.DodgerBlue);
        _messageBrush = (Brush)(TryFindResource("TextPrimaryBrush") ?? Brushes.Black);

        LogBox.Document.Blocks.Clear();
        _viewModel.Logs.CollectionChanged += OnLogsCollectionChanged;

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            await _viewModel.RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load status:\n\n{ex.Message}",
                "RDPExtender",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                LogBox.Document.Blocks.Clear();
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                foreach (LogEntry entry in e.NewItems)
                {
                    AppendLogEntry(entry);
                }
                LogBox.ScrollToEnd();
            }
        }));
    }

    private void AppendLogEntry(LogEntry entry)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 2),
            LineHeight = 1.0,
        };

        paragraph.Inlines.Add(new Run(entry.Timestamp)
        {
            Foreground = _timestampBrush,
            FontWeight = FontWeights.SemiBold,
        });

        paragraph.Inlines.Add(new Run("   " + entry.Message)
        {
            Foreground = _messageBrush,
        });

        LogBox.Document.Blocks.Add(paragraph);
    }
}
