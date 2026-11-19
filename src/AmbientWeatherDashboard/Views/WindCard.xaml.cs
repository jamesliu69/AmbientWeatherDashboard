using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>顯示目前風向、風速與陣風資訊的儀表板卡片。</summary>
public partial class WindCard : UserControl
{
    /// <summary>載入風況卡片的 XAML 元件。</summary>
    public WindCard() => InitializeComponent();
}
