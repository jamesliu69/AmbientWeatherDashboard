using System.Windows.Controls;

namespace AmbientWeatherDashboard.Views;

/// <summary>提供溫度、風速、語言與減少動畫設定的檢視。</summary>
public partial class SettingsView : UserControl
{
    /// <summary>載入設定檢視的 XAML 元件。</summary>
    public SettingsView() => InitializeComponent();
}
