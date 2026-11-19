using System.Runtime.InteropServices;
using System.Windows;

namespace AmbientWeatherDashboard;

/// <summary>透過 Windows User32 API 查詢視窗所在顯示器的工作區像素尺寸。</summary>
internal static class MonitorWorkArea
{
    /// <summary>使用視窗控制代碼呼叫 User32 MonitorFromWindow，取得顯示器控制代碼。</summary>
    internal static nint NearestMonitor(nint windowHandle) => MonitorFromWindow(windowHandle, 2);

    /// <summary>取得指定顯示器的工作區像素寬高，API 失敗時回傳 null。</summary>
    internal static Size? GetPixelSize(nint monitor)
    {
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
            return null;

        return new Size(info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
    }

    /// <summary>將視窗控制代碼與旗標傳入 User32 MonitorFromWindow，回傳其選定的顯示器控制代碼。</summary>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern nint MonitorFromWindow(nint hwnd, uint flags);

    /// <summary>呼叫 User32 取得顯示器及工作區矩形與顯示器狀態旗標。</summary>
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    /// <summary>依 GetMonitorInfoW 原生欄位順序保存顯示器尺寸、監視器與工作區矩形及旗標。</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        /// <summary>傳入原生 API 的結構大小，單位為位元組。</summary>
        public int Size;
        /// <summary>顯示器完整範圍的原生矩形。</summary>
        public NativeRect Monitor;
        /// <summary>排除工作列等系統介面後的可用範圍。</summary>
        public NativeRect Work;
        /// <summary>由原生 API 回傳的顯示器狀態旗標。</summary>
        public uint Flags;
    }

    /// <summary>依 Windows RECT 欄位順序保存左、上、右、下座標。</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        /// <summary>矩形左側的水平座標。</summary>
        public int Left;
        /// <summary>矩形上側的垂直座標。</summary>
        public int Top;
        /// <summary>矩形右側的水平座標。</summary>
        public int Right;
        /// <summary>矩形下側的垂直座標。</summary>
        public int Bottom;
    }
}
