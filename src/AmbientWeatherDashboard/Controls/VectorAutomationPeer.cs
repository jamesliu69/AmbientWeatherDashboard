using System.Windows.Automation.Peers;

namespace AmbientWeatherDashboard.Controls;

internal sealed class VectorAutomationPeer(VectorControl owner) : FrameworkElementAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
    protected override string GetClassNameCore() => Owner.GetType().Name;
}
