using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xhair.Models;
using Xhair.ViewModels;
using Xhair.Services;

namespace Xhair;

public partial class SettingsWindow : Window
{
    private bool _drawModeEnabled;
    private bool _isDrawing;
    private Polyline? _activeStroke;
    private bool _isDraggingImage;
    private System.Windows.Point _dragStart;
    private System.Windows.Point _imageStart;
    private bool _captureToggleHotkey;
    private bool _captureCycleHotkey;

    public SettingsWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private OverlayViewModel? ViewModel => DataContext as OverlayViewModel;

    private void OnLoadProfile(object sender, RoutedEventArgs e)
    {
        ViewModel?.LoadSelectedProfile();
    }

    private void OnSaveProfile(object sender, RoutedEventArgs e)
    {
        ViewModel?.SaveCurrentProfile();
    }

    private void OnAddProfile(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddProfile();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        WireViewModel(ViewModel);
        UpdateEditorGuides();
        UpdateEditorPreview();
        RenderEditStrokes();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is OverlayViewModel oldViewModel)
        {
            oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        WireViewModel(ViewModel);
        UpdateEditorGuides();
        UpdateEditorPreview();
        RenderEditStrokes();
    }

    private void WireViewModel(OverlayViewModel? viewModel)
    {
        if (viewModel == null)
        {
            return;
        }

        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OverlayViewModel.ShowEditorGuides))
        {
            Dispatcher.Invoke(UpdateEditorGuides);
        }
        else if (e.PropertyName == nameof(OverlayViewModel.EditStrokes))
        {
            Dispatcher.Invoke(RenderEditStrokes);
        }
        else
        {
            Dispatcher.Invoke(UpdateEditorPreview);
        }
    }

    private void OnEditorSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateEditorGuides();
        UpdateEditorPreview();
    }

    private void UpdateEditorGuides()
    {
        if (EditorCanvas == null)
        {
            return;
        }

        VerticalGuide.X1 = 0;
        VerticalGuide.X2 = 0;
        VerticalGuide.Y1 = 0;
        VerticalGuide.Y2 = EditorCanvas.ActualHeight;
        System.Windows.Controls.Canvas.SetLeft(VerticalGuide, EditorCanvas.ActualWidth / 2);

        HorizontalGuide.X1 = 0;
        HorizontalGuide.X2 = EditorCanvas.ActualWidth;
        HorizontalGuide.Y1 = 0;
        HorizontalGuide.Y2 = 0;
        System.Windows.Controls.Canvas.SetTop(HorizontalGuide, EditorCanvas.ActualHeight / 2);
    }

    private void UpdateEditorPreview()
    {
        if (ViewModel == null || EditorCanvas == null)
        {
            return;
        }

        double centerX = EditorCanvas.ActualWidth / 2 + ViewModel.EditOffsetX;
        double centerY = EditorCanvas.ActualHeight / 2 + ViewModel.EditOffsetY;

        System.Windows.Controls.Canvas.SetLeft(PreviewLeft, centerX - ViewModel.EditGap - ViewModel.EditLineLength);
        System.Windows.Controls.Canvas.SetTop(PreviewLeft, centerY - ViewModel.EditThickness / 2);

        System.Windows.Controls.Canvas.SetLeft(PreviewRight, centerX + ViewModel.EditGap);
        System.Windows.Controls.Canvas.SetTop(PreviewRight, centerY - ViewModel.EditThickness / 2);

        System.Windows.Controls.Canvas.SetLeft(PreviewTop, centerX - ViewModel.EditThickness / 2);
        System.Windows.Controls.Canvas.SetTop(PreviewTop, centerY - ViewModel.EditGap - ViewModel.EditLineLength);

        System.Windows.Controls.Canvas.SetLeft(PreviewBottom, centerX - ViewModel.EditThickness / 2);
        System.Windows.Controls.Canvas.SetTop(PreviewBottom, centerY + ViewModel.EditGap);

        System.Windows.Controls.Canvas.SetLeft(PreviewDot, centerX - ViewModel.EditDotSize / 2);
        System.Windows.Controls.Canvas.SetTop(PreviewDot, centerY - ViewModel.EditDotSize / 2);
    }

    private void RenderEditStrokes()
    {
        if (ViewModel == null || EditorCanvas == null)
        {
            return;
        }

        for (int i = EditorCanvas.Children.Count - 1; i >= 0; i--)
        {
            if (EditorCanvas.Children[i] is Shape shape && shape.Tag as string == "DrawStroke")
            {
                EditorCanvas.Children.RemoveAt(i);
            }
        }

        foreach (EditorStroke stroke in ViewModel.EditStrokes)
        {
            if (stroke.Points.Count == 0)
            {
                continue;
            }

            var polyline = new Polyline
            {
                Stroke = new SolidColorBrush(ViewModel.EditCrosshairBrush.Color),
                StrokeThickness = Math.Max(1, ViewModel.EditThickness / 2),
                Opacity = 0.8,
                Tag = "DrawStroke"
            };

            foreach (EditorPoint point in stroke.Points)
            {
                polyline.Points.Add(new System.Windows.Point(point.X, point.Y));
            }

            System.Windows.Controls.Panel.SetZIndex(polyline, 6);
            EditorCanvas.Children.Add(polyline);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (App.IsQuitting)
        {
            return;
        }

        MessageBoxResult result = System.Windows.MessageBox.Show(
            "Run in the tray so your crosshair stays active?",
            "Close Xhair",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        App.RequestShutdown();
    }

    private void OnDrawModeChecked(object sender, RoutedEventArgs e)
    {
        _drawModeEnabled = true;
        EditorCanvas.Cursor = System.Windows.Input.Cursors.Pen;
    }

    private void OnDrawModeUnchecked(object sender, RoutedEventArgs e)
    {
        _drawModeEnabled = false;
        EditorCanvas.Cursor = System.Windows.Input.Cursors.Arrow;
    }

    private void OnEditorProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel?.LoadSelectedProfile();
    }

    private void OnApplyEditor(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyEditorSettings();
    }

    private void OnEditorMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_drawModeEnabled || ViewModel == null)
        {
            return;
        }

        _isDrawing = true;
        System.Windows.Point point = e.GetPosition(EditorCanvas);
        _activeStroke = new Polyline
        {
            Stroke = new SolidColorBrush(ViewModel.CrosshairBrush.Color),
            StrokeThickness = Math.Max(1, ViewModel.Thickness / 2),
            Opacity = 0.8,
            Tag = "DrawStroke"
        };
        _activeStroke.Points.Add(point);
        System.Windows.Controls.Panel.SetZIndex(_activeStroke, 6);
        EditorCanvas.Children.Add(_activeStroke);
        EditorCanvas.CaptureMouse();
    }

    private void OnEditorMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDrawing || _activeStroke == null)
        {
            return;
        }

        _activeStroke.Points.Add(e.GetPosition(EditorCanvas));
    }

    private void OnEditorMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDrawing)
        {
            return;
        }

        _isDrawing = false;
        if (ViewModel != null && _activeStroke != null)
        {
            var stroke = new EditorStroke
            {
                Points = _activeStroke.Points.Select(point => new EditorPoint { X = point.X, Y = point.Y }).ToList()
            };
            var strokes = ViewModel.EditStrokes.ToList();
            strokes.Add(stroke);
            ViewModel.EditStrokes = strokes;
        }
        _activeStroke = null;
        EditorCanvas.ReleaseMouseCapture();
    }

    private void OnClearDrawing(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.EditStrokes = new List<EditorStroke>();
        }

        for (int i = EditorCanvas.Children.Count - 1; i >= 0; i--)
        {
            if (EditorCanvas.Children[i] is Shape shape && shape.Tag as string == "DrawStroke")
            {
                EditorCanvas.Children.RemoveAt(i);
            }
        }
    }

    private void OnEditorDragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = HasPngDrop(e) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void OnEditorDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        string? path = GetFirstPngDrop(e);
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        ViewModel.EditorImagePath = path;
        CenterEditorImage(path);
    }

    private void CenterEditorImage(string path)
    {
        if (ViewModel == null || EditorCanvas == null)
        {
            return;
        }

        if (!File.Exists(path))
        {
            return;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();

        double width = image.PixelWidth * 96.0 / image.DpiX;
        double height = image.PixelHeight * 96.0 / image.DpiY;
        ViewModel.EditorImageX = (EditorCanvas.ActualWidth - width) / 2;
        ViewModel.EditorImageY = (EditorCanvas.ActualHeight - height) / 2;
    }

    private static bool HasPngDrop(System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return false;
        }

        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return false;
        }

        string ext = System.IO.Path.GetExtension(files[0]);
        return string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetFirstPngDrop(System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            return null;
        }

        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return null;
        }

        string ext = System.IO.Path.GetExtension(files[0]);
        return string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase) ? files[0] : null;
    }

    private void OnRemoveImage(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        ViewModel.EditorImagePath = null;
        ViewModel.EditorImageX = 0;
        ViewModel.EditorImageY = 0;
    }

    private void OnPresetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            return;
        }

        if (e.AddedItems[0] is CrosshairPreset preset)
        {
            ViewModel?.ApplyPresetToEditor(preset);
        }
    }

    private void OnRenameProfile(object sender, RoutedEventArgs e)
    {
        ViewModel?.RenameSelectedProfile();
    }

    private void OnDuplicateProfile(object sender, RoutedEventArgs e)
    {
        ViewModel?.DuplicateSelectedProfile();
    }

    private void OnImportProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Xhair Profile (*.xhair)|*.xhair",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            ViewModel?.ImportProfile(dialog.FileName);
        }
    }

    private void OnExportProfile(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Xhair Profile (*.xhair)|*.xhair",
            FileName = ViewModel?.SelectedProfileName ?? "profile"
        };

        if (dialog.ShowDialog(this) == true)
        {
            ViewModel?.ExportProfile(dialog.FileName);
        }
    }

    private void OnSetToggleHotkey(object sender, RoutedEventArgs e)
    {
        _captureToggleHotkey = true;
        _captureCycleHotkey = false;
        SetHotkeyHint("Press a key to set Toggle Crosshair hotkey.");
    }

    private void OnSetCycleHotkey(object sender, RoutedEventArgs e)
    {
        _captureToggleHotkey = false;
        _captureCycleHotkey = true;
        SetHotkeyHint("Press a key to set Cycle Profiles hotkey.");
    }

    private void OnHotkeyPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (!_captureToggleHotkey && !_captureCycleHotkey)
        {
            return;
        }

        int vk = KeyInterop.VirtualKeyFromKey(e.Key == Key.System ? e.SystemKey : e.Key);
        if (vk == 0 || ViewModel == null)
        {
            return;
        }

        var key = (System.Windows.Forms.Keys)vk;
        if (_captureToggleHotkey)
        {
            ViewModel.ToggleHotkey = key;
        }
        else if (_captureCycleHotkey)
        {
            ViewModel.CycleHotkey = key;
        }

        _captureToggleHotkey = false;
        _captureCycleHotkey = false;
        SetHotkeyHint(string.Empty);
        e.Handled = true;
    }

    private void SetHotkeyHint(string text)
    {
        if (FindName("HotkeyCaptureHint") is TextBlock hint)
        {
            hint.Text = text;
        }
    }

    private void OnStartupToggleChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null)
        {
            return;
        }

        App.ApplyStartupSetting(ViewModel.StartWithWindows);
    }

    private async void OnUpdateClicked(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null || string.IsNullOrWhiteSpace(ViewModel.LatestInstallerUrl))
        {
            return;
        }

        string? installerPath = await UpdateService.DownloadInstallerAsync(ViewModel.LatestInstallerUrl);
        if (string.IsNullOrWhiteSpace(installerPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
        App.RequestShutdown();
    }

    private void OnImageMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel == null || _drawModeEnabled)
        {
            return;
        }

        _isDraggingImage = true;
        _dragStart = e.GetPosition(EditorCanvas);
        _imageStart = new System.Windows.Point(ViewModel.EditorImageX, ViewModel.EditorImageY);
        PreviewImage.CaptureMouse();
        e.Handled = true;
    }

    private void OnImageMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isDraggingImage || ViewModel == null)
        {
            return;
        }

        System.Windows.Point current = e.GetPosition(EditorCanvas);
        Vector delta = current - _dragStart;
        ViewModel.EditorImageX = _imageStart.X + delta.X;
        ViewModel.EditorImageY = _imageStart.Y + delta.Y;
    }

    private void OnImageMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingImage)
        {
            return;
        }

        _isDraggingImage = false;
        PreviewImage.ReleaseMouseCapture();
    }
}
