using System.Windows.Automation.Peers;

namespace AmbientWeatherDashboard.Controls;

/// <summary>將向量控制項呈報為 UI Automation 影像並沿用控制項型別名稱。</summary>
internal sealed class VectorAutomationPeer(VectorControl owner) : FrameworkElementAutomationPeer(owner)
{
    /// <summary>將此向量控制項的自動化類別標示為影像。</summary>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
    /// <summary>回傳所屬控制項的執行期型別名稱。</summary>
    protected override string GetClassNameCore() => Owner.GetType().Name;
}
