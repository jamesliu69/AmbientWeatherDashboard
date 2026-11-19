using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>顯示目前溫度、露點、體感溫度與昨日高溫的卡片。</summary>
public partial class TemperatureCard : UserControl
{
    /// <summary>載入溫度卡片的 XAML 元件。</summary>
    public TemperatureCard() => InitializeComponent();
}
