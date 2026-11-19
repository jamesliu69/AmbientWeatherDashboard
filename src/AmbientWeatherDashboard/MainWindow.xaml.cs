using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using AmbientWeatherDashboard.ViewModels;

namespace AmbientWeatherDashboard;

/// <summary>管理主視窗生命週期、時鐘更新、工作區尺寸及視窗狀態。</summary>
public partial class MainWindow : Window
{
    /// <summary>提供主視窗使用的應用程式 ViewModel。</summary>
    private readonly MainViewModel viewModel;
    /// <summary>每秒觸發一次儀表板時鐘更新的 DispatcherTimer。</summary>
    private readonly DispatcherTimer clockTimer = new(DispatcherPriority.Background);
    /// <summary>記錄最近一次計算工作區尺寸所使用的顯示器代碼。</summary>
    private nint lastMonitor;
    /// <summary>記錄最近一次計算工作區尺寸所使用的 DPI 比例。</summary>
    private DpiScale lastDpi;
    /// <summary>標記非同步關閉流程已開始，避免重複進入。</summary>
    private bool stopping;
    /// <summary>標記可放行後續 Close 事件，避免再次取消關閉。</summary>
    private bool stopped;

    /// <summary>設定資料上下文、視窗事件、工作區尺寸追蹤及時鐘計時器。</summary>
    public MainWindow(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
        Loaded += Start;
        Closing += Stop;
        StateChanged += UpdateWindowState;
        SourceInitialized += (_, _) => FitWorkArea(VisualTreeHelper.GetDpi(this));
        LocationChanged += (_, _) => FitWorkArea(VisualTreeHelper.GetDpi(this));
        DpiChanged += (_, e) => FitWorkArea(e.NewDpi);
        clockTimer.Interval = TimeSpan.FromSeconds(1);
        clockTimer.Tick += UpdateClock;
    }

    /// <summary>視窗載入後啟動時鐘並開始 ViewModel 的資料更新。</summary>
    private async void Start(object sender, RoutedEventArgs e)
    {
        Loaded -= Start;
        clockTimer.Start();
        await viewModel.StartAsync();
    }

    /// <summary>攔截第一次關閉，停止更新後再排程完成關閉，並記錄停止時的例外。</summary>
    private async void Stop(object? sender, CancelEventArgs e)
    {
        if (stopped)
            return;

        e.Cancel = true;
        if (stopping)
            return;

        stopping = true;
        clockTimer.Stop();
        clockTimer.Tick -= UpdateClock;
        try
        {
            await viewModel.StopAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError("Application shutdown failed: {0}", ex);
        }
        finally
        {
            stopped = true;
            // Close after the original Closing event has returned.
            // 原始 Closing 事件回呼返回後，再透過 Dispatcher 排程呼叫 Close。
            _ = Dispatcher.BeginInvoke(Close);
        }
    }

    /// <summary>將計時器事件轉交給儀表板 ViewModel 更新時鐘。</summary>
    private void UpdateClock(object? sender, EventArgs e) => viewModel.Dashboard.Tick();

    /// <summary>依所在顯示器與 DPI 換算工作區尺寸，並限制視窗最小及目前尺寸。</summary>
    private void FitWorkArea(DpiScale dpi, bool force = false)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == 0)
            return;

        var monitor = MonitorWorkArea.NearestMonitor(hwnd);
        if (!force && monitor == lastMonitor && dpi.DpiScaleX == lastDpi.DpiScaleX && dpi.DpiScaleY == lastDpi.DpiScaleY)
            return;
        if (MonitorWorkArea.GetPixelSize(monitor) is not { } pixels)
            return;

        lastMonitor = monitor;
        lastDpi = dpi;
        var width = Math.Floor(pixels.Width / dpi.DpiScaleX);
        var height = Math.Floor(pixels.Height / dpi.DpiScaleY);
        // High DPI can leave less room than the nominal dashboard minimum.
        // 高 DPI 換算後的工作區可能小於儀表板名義上的最小尺寸。
        MinWidth = Math.Min(1280, width);
        MinHeight = Math.Min(760, height);
        if (WindowState == WindowState.Normal)
        {
            Width = Math.Min(Width, width);
            Height = Math.Min(Height, height);
        }
    }

    /// <summary>更新最大化按鈕圖示與最小化狀態，還原時重新計算所在工作區。</summary>
    private void UpdateWindowState(object? sender, EventArgs e)
    {
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        viewModel.Dashboard.SetMinimized(WindowState == WindowState.Minimized);
        if (WindowState == WindowState.Normal)
            FitWorkArea(VisualTreeHelper.GetDpi(this), force: true);
    }

    /// <summary>透過 WPF 系統命令將視窗最小化。</summary>
    private void MinimizeClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    /// <summary>依目前狀態最大化或還原視窗。</summary>
    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    /// <summary>透過 WPF 系統命令關閉視窗。</summary>
    private void CloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
