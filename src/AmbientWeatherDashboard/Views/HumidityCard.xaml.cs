using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>顯示目前濕度及相較昨日同一時刻的變化。</summary>
public partial class HumidityCard : UserControl
{
    /// <summary>載入濕度卡片的 XAML 元件。</summary>
    public HumidityCard() => InitializeComponent();
}
