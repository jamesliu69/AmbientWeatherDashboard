using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AmbientWeatherDashboard.ViewModels;

/// <summary>提供太陽輻射、紫外線或光照指標的顯示資料。</summary>
public sealed partial class SunMetricViewModel(double maximum, Brush accentBrush, string unit) : ObservableObject
{
    /// <summary>保存指標刻度的最大值。</summary>
    public double Maximum { get; } = maximum;
    /// <summary>保存指標刻度使用的強調色筆刷。</summary>
    public Brush AccentBrush { get; } = accentBrush;
    /// <summary>保存指標讀值旁顯示的單位文字。</summary>
    public string Unit { get; } = unit;
    public string Accessible => $"{Title} {Readout} {Unit}. {Detail}";

    /// <summary>保存指標標題，變更時同步通知無障礙描述更新。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Accessible))]
    private string title = "";

    /// <summary>保存未格式化的數值，缺少時為 null。</summary>
    [ObservableProperty]
    private double? value;

    /// <summary>保存格式化後的主要讀值。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Accessible))]
    private string readout = "—";

    /// <summary>保存補充指標資訊，並納入無障礙描述。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Accessible))]
    private string detail = "";

    /// <summary>一次更新指標標題、數值、讀值文字與補充說明。</summary>
    public void Display(string title, double? number, string formatted, string detail)
    {
        Title = title;
        Value = number;
        Readout = formatted;
        Detail = detail;
    }
}
