using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>以分段儀表顯示太陽輻射、紫外線和光照指標的卡片。</summary>
public partial class SunCard : UserControl
{
    /// <summary>載入日照指標卡片的 XAML 元件。</summary>
    public SunCard() => InitializeComponent();
}
