using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Forms;
using Xhair.ViewModels;

namespace Xhair;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        SourceInitialized += OnSourceInitialized;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SetToActiveMonitor();
        UpdateViewportSize();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateViewportSize();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        int exStyle = GetWindowLong(handle, GwlExstyle);
        SetWindowLong(handle, GwlExstyle, exStyle | WsExTransparent | WsExLayered);
    }

    private void SetToActiveMonitor()
    {
        System.Drawing.Point cursor = System.Windows.Forms.Cursor.Position;
        Screen screen = Screen.FromPoint(cursor);
        Left = screen.Bounds.Left;
        Top = screen.Bounds.Top;
        Width = screen.Bounds.Width;
        Height = screen.Bounds.Height;
    }

    private void UpdateViewportSize()
    {
        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.ViewportWidth = ActualWidth;
            viewModel.ViewportHeight = ActualHeight;
        }
    }

    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExLayered = 0x00080000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}