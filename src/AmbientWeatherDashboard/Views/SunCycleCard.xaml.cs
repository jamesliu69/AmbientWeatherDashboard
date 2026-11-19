using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>顯示日照時長、目前日照進度、日出與日落時間的卡片。</summary>
public partial class SunCycleCard : UserControl
{
    /// <summary>載入日照週期卡片的 XAML 元件。</summary>
    public SunCycleCard() => InitializeComponent();
}
