using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media;
using AmbientWeatherDashboard.ViewModels;

namespace AmbientWeatherDashboard;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly DispatcherTimer clockTimer = new(DispatcherPriority.Background);
    private nint lastMonitor;
    private DpiScale lastDpi;
    private bool stopping;
    private bool stopped;

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

    private async void Start(object sender, RoutedEventArgs e)
    {
        Loaded -= Start;
        clockTimer.Start();
        await viewModel.StartAsync();
    }

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
            _ = Dispatcher.BeginInvoke(Close);
        }
    }

    private void UpdateClock(object? sender, EventArgs e) => viewModel.Dashboard.Tick();

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
        MinWidth = Math.Min(1280, width);
        MinHeight = Math.Min(760, height);
        if (WindowState == WindowState.Normal)
        {
            Width = Math.Min(Width, width);
            Height = Math.Min(Height, height);
        }
    }

    private void UpdateWindowState(object? sender, EventArgs e)
    {
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        viewModel.Dashboard.SetMinimized(WindowState == WindowState.Minimized);
        if (WindowState == WindowState.Normal)
            FitWorkArea(VisualTreeHelper.GetDpi(this), force: true);
    }

    private void MinimizeClick(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            SystemCommands.RestoreWindow(this);
        else
            SystemCommands.MaximizeWindow(this);
    }

    private void CloseClick(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
