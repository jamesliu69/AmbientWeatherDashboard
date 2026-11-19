using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>將今日、預報、風況、濕度、日照與溫度卡片排成儀表板。</summary>
public partial class DashboardView : UserControl
{
    /// <summary>載入儀表板版面中的 XAML 元件。</summary>
    public DashboardView() => InitializeComponent();
}
