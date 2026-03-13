using System;
using System.Windows;
using Xhair.ViewModels;

namespace Xhair;

public partial class QuickSettingsWindow : Window
{
    public QuickSettingsWindow()
    {
        InitializeComponent();
        Deactivated += (_, _) => Hide();
    }

    public event EventHandler? OpenAppRequested;

    private OverlayViewModel? ViewModel => DataContext as OverlayViewModel;

    private void OnProfileChanged(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedProfileName != null)
        {
            ViewModel.ApplyProfileToOverlay(ViewModel.SelectedProfileName);
        }
    }

    private void OnOpenApp(object sender, RoutedEventArgs e)
    {
        Hide();
        OpenAppRequested?.Invoke(this, EventArgs.Empty);
    }

}
