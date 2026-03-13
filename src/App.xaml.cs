using System.IO;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Resources;
using System.Drawing;
using System.Reflection;
using Xhair.Models;
using Xhair.Services;
using Xhair.ViewModels;
using WinForms = System.Windows.Forms;
using Microsoft.Win32;

namespace Xhair;


public partial class App : System.Windows.Application
{
	internal static bool IsQuitting { get; private set; }

	private WinForms.NotifyIcon? _trayIcon;
	private WinForms.ToolStripMenuItem? _toggleOverlayItem;
	private MainWindow? _overlayWindow;
	private SettingsWindow? _settingsWindow;
	private QuickSettingsWindow? _quickSettingsWindow;
	private GlobalHotkey? _hotkey;
	private GlobalHotkey? _cycleHotkey;
	private OverlayViewModel? _viewModel;
	private ProfileStorage? _profileStorage;
    private Icon? _trayIconHandle;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		ShutdownMode = ShutdownMode.OnExplicitShutdown;

		_profileStorage = new ProfileStorage();
		ProfileStore store = _profileStorage.Load();
		_viewModel = new OverlayViewModel(store, _profileStorage);
		_viewModel.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == nameof(OverlayViewModel.IsEnabled))
			{
				UpdateTrayToggleItem();
			}
			else if (args.PropertyName == nameof(OverlayViewModel.ToggleHotkey)
				|| args.PropertyName == nameof(OverlayViewModel.CycleHotkey))
			{
				UpdateHotkeys();
			}
		};

		_overlayWindow = new MainWindow
		{
			DataContext = _viewModel
		};
		_overlayWindow.Show();

		UpdateHotkeys();

		InitializeTrayIcon();
		ApplyStartupSetting(_viewModel.StartWithWindows);
		if (!_viewModel.StartInTray)
		{
			ShowSettings();
		}

		_ = CheckForUpdatesAsync();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_viewModel?.SaveCurrentProfile();
		_hotkey?.Dispose();
		_cycleHotkey?.Dispose();
		_trayIcon?.Dispose();
		_trayIconHandle?.Dispose();
		base.OnExit(e);
	}

	private void InitializeTrayIcon()
	{
		_trayIcon = new WinForms.NotifyIcon
		{
			Icon = LoadTrayIcon() ?? System.Drawing.SystemIcons.Application,
			Visible = true,
			Text = "Xhair"
		};

		var menu = new WinForms.ContextMenuStrip();
		menu.Items.Add("Quick Settings", null, (_, _) => ShowQuickSettings());
		menu.Items.Add("Settings", null, (_, _) => ShowSettings());
		_toggleOverlayItem = new WinForms.ToolStripMenuItem("Disable Crosshair", null, (_, _) => ToggleOverlay());
		menu.Items.Add(_toggleOverlayItem);
		menu.Items.Add("Quit", null, (_, _) => RequestShutdown());
		_trayIcon.ContextMenuStrip = menu;
		_trayIcon.MouseClick += (_, args) =>
		{
			if (args.Button == WinForms.MouseButtons.Left)
			{
				ShowQuickSettings();
			}
		};

		UpdateTrayToggleItem();
	}

	private void ShowQuickSettings()
	{
		if (_viewModel == null)
		{
			return;
		}

		if (_quickSettingsWindow == null)
		{
			_quickSettingsWindow = new QuickSettingsWindow
			{
				DataContext = _viewModel
			};
			_quickSettingsWindow.OpenAppRequested += (_, _) => ShowSettings();
			_quickSettingsWindow.Closed += (_, _) => _quickSettingsWindow = null;
		}

		if (_quickSettingsWindow.IsVisible)
		{
			_quickSettingsWindow.Hide();
			return;
		}

		_quickSettingsWindow.Show();
		_quickSettingsWindow.UpdateLayout();
		PositionQuickSettingsWindow(_quickSettingsWindow);
		_quickSettingsWindow.Activate();
	}

	private static void PositionQuickSettingsWindow(Window window)
	{
		WinForms.Screen screen = WinForms.Screen.FromPoint(WinForms.Cursor.Position);
		System.Drawing.Rectangle workingArea = screen.WorkingArea;
		PresentationSource? presentationSource = PresentationSource.FromVisual(window);
		Matrix transform = presentationSource?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
		System.Windows.Point bottomRight = transform.Transform(new System.Windows.Point(workingArea.Right, workingArea.Bottom));
		window.Left = bottomRight.X - window.ActualWidth - 12;
		window.Top = bottomRight.Y - window.ActualHeight - 12;
	}

	private void ShowSettings()
	{
		if (_viewModel == null)
		{
			return;
		}

		_quickSettingsWindow?.Hide();

		if (_settingsWindow == null)
		{
			_settingsWindow = new SettingsWindow
			{
				DataContext = _viewModel
			};
			_settingsWindow.Closed += (_, _) => _settingsWindow = null;
		}

		_settingsWindow.Show();
		_settingsWindow.Activate();
	}

	private void ToggleOverlay()
	{
		if (_viewModel == null)
		{
			return;
		}

		Dispatcher.Invoke(() => { _viewModel.IsEnabled = !_viewModel.IsEnabled; });
	}

	internal static void RequestShutdown()
	{
		IsQuitting = true;
		Current?.Shutdown();
	}

	private void UpdateTrayToggleItem()
	{
		if (_toggleOverlayItem == null || _viewModel == null)
		{
			return;
		}

		if (_viewModel.IsEnabled)
		{
			_toggleOverlayItem.Text = "Disable Crosshair";
			_toggleOverlayItem.Checked = true;
		}
		else
		{
			_toggleOverlayItem.Text = "Enable Crosshair";
			_toggleOverlayItem.Checked = false;
		}
	}

	private void UpdateHotkeys()
	{
		_hotkey?.Dispose();
		_cycleHotkey?.Dispose();

		if (_viewModel == null)
		{
			return;
		}

		_hotkey = new GlobalHotkey(_viewModel.ToggleHotkey, ToggleOverlay);
		_cycleHotkey = new GlobalHotkey(_viewModel.CycleHotkey, CycleProfile);
	}

	private void CycleProfile()
	{
		if (_viewModel == null || _viewModel.Profiles.Count == 0)
		{
			return;
		}

		Dispatcher.Invoke(() =>
		{
			int index = _viewModel.Profiles.IndexOf(_viewModel.SelectedProfileName);
			int nextIndex = index < 0 ? 0 : (index + 1) % _viewModel.Profiles.Count;
			string nextProfile = _viewModel.Profiles[nextIndex];
			_viewModel.ApplyProfileToOverlay(nextProfile);
		});
	}

	internal static void ApplyStartupSetting(bool enable)
	{
		const string runKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
		using RegistryKey? key = Registry.CurrentUser.OpenSubKey(runKey, true);
		if (key == null)
		{
			return;
		}

		string appName = "Xhair";
		string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
		if (string.IsNullOrWhiteSpace(exePath))
		{
			return;
		}

		if (enable)
		{
			key.SetValue(appName, '"' + exePath + '"');
		}
		else
		{
			key.DeleteValue(appName, false);
		}
	}

	private Icon? LoadTrayIcon()
	{
		StreamResourceInfo? resource = GetResourceStream(new Uri("pack://application:,,,/logoblack.png"));
		if (resource == null)
		{
			return null;
		}

		using var stream = resource.Stream;
		using var bitmap = new Bitmap(stream);
		IntPtr hIcon = bitmap.GetHicon();
		try
		{
			_trayIconHandle = Icon.FromHandle(hIcon);
			return (Icon)_trayIconHandle.Clone();
		}
		finally
		{
			DestroyIcon(hIcon);
		}
	}

	[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
	private static extern bool DestroyIcon(IntPtr hIcon);

	private async Task CheckForUpdatesAsync()
	{
		if (_viewModel == null)
		{
			return;
		}

		try
		{
			string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
			UpdateInfo info = await UpdateService.CheckForUpdateAsync(version);
			Dispatcher.Invoke(() =>
			{
				_viewModel.IsUpdateAvailable = info.IsUpdateAvailable;
				_viewModel.LatestReleaseTag = info.LatestTag;
				_viewModel.LatestInstallerUrl = info.InstallerUrl;
			});
		}
		catch
		{
			// Ignore update check failures.
		}
	}

    
}

