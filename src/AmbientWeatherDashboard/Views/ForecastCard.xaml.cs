using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>顯示焦點預報圖示與逐時預報資料的卡片。</summary>
public partial class ForecastCard : UserControl
{
    /// <summary>載入逐時預報卡片的 XAML 元件。</summary>
    public ForecastCard() => InitializeComponent();
}
