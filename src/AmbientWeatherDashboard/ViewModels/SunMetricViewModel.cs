using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AmbientWeatherDashboard.ViewModels;

public sealed partial class SunMetricViewModel(double maximum, Brush accentBrush, string unit) : ObservableObject
{
    public double Maximum { get; } = maximum;
    public Brush AccentBrush { get; } = accentBrush;
    public string Unit { get; } = unit;
    public string Accessible => $"{Title} {Readout} {Unit}. {Detail}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Accessible))]
    private string title = "";

    [ObservableProperty]
    private double? value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Accessible))]
    private string readout = "—";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Accessible))]
    private string detail = "";

    public void Display(string title, double? number, string formatted, string detail)
    {
        Title = title;
        Value = number;
        Readout = formatted;
        Detail = detail;
    }
}
