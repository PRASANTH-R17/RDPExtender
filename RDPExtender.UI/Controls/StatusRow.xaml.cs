using System.Windows;
using System.Windows.Controls;
using RDPExtender.Models;

namespace RDPExtender.UI.Controls;

public partial class StatusRow : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(StatusRow));

    public static readonly DependencyProperty RowIconGlyphProperty =
        DependencyProperty.Register(nameof(RowIconGlyph), typeof(string), typeof(StatusRow));

    public static readonly DependencyProperty StatusTextProperty =
        DependencyProperty.Register(nameof(StatusText), typeof(string), typeof(StatusRow));

    public static readonly DependencyProperty LevelProperty =
        DependencyProperty.Register(nameof(Level), typeof(StatusLevel), typeof(StatusRow));

    public StatusRow()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string RowIconGlyph
    {
        get => (string)GetValue(RowIconGlyphProperty);
        set => SetValue(RowIconGlyphProperty, value);
    }

    public string StatusText
    {
        get => (string)GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public StatusLevel Level
    {
        get => (StatusLevel)GetValue(LevelProperty);
        set => SetValue(LevelProperty, value);
    }
}
