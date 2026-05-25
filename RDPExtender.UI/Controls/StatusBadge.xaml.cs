using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RDPExtender.Models;

namespace RDPExtender.UI.Controls;

public partial class StatusBadge : UserControl
{
    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(StatusLevel), typeof(StatusBadge),
            new PropertyMetadata(StatusLevel.Warning, OnAppearanceChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(StatusBadge),
            new PropertyMetadata(string.Empty, OnAppearanceChanged));

    public StatusBadge()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyAppearance();
    }

    public StatusLevel Level
    {
        get => (StatusLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBadge badge)
        {
            badge.ApplyAppearance();
        }
    }

    private void ApplyAppearance()
    {
        if (BadgeText is null || BadgeIcon is null || BadgeBorder is null)
        {
            return;
        }

        BadgeText.Text = Text;
        BadgeBorder.Background = GetBrush(Level switch
        {
            StatusLevel.Ok => "SuccessBgBrush",
            StatusLevel.Error => "ErrorBgBrush",
            _ => "WarningBgBrush"
        });

        var fg = GetBrush(Level switch
        {
            StatusLevel.Ok => "SuccessFgBrush",
            StatusLevel.Error => "ErrorFgBrush",
            _ => "WarningFgBrush"
        });

        BadgeText.Foreground = fg;
        BadgeIcon.Foreground = fg;
        BadgeIcon.Text = Level switch
        {
            StatusLevel.Ok => "\uE73E",
            StatusLevel.Error => "\uE711",
            _ => "\uE915"
        };
    }

    private static Brush GetBrush(string key)
    {
        return Application.Current?.TryFindResource(key) as Brush
            ?? Brushes.Gray;
    }
}
