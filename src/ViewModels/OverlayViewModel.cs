using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Xhair.Models;
using Xhair.Services;
using WinForms = System.Windows.Forms;

namespace Xhair.ViewModels;

public sealed class OverlayViewModel : INotifyPropertyChanged
{
    private readonly ProfileStore _store;
    private readonly ProfileStorage _storage;
    private SolidColorBrush _crosshairBrush;
    private SolidColorBrush _editCrosshairBrush;
    private SolidColorBrush _outlineBrush;
    private SolidColorBrush _editOutlineBrush;

    private string _selectedProfileName;
    private string _newProfileName = string.Empty;
    private string _renameProfileName = string.Empty;
    private double _viewportWidth;
    private double _viewportHeight;

    private string _colorHex = "#FFFFFF";
    private string _outlineColorHex = "#000000";
    private double _lineLength;
    private double _thickness;
    private double _gap;
    private double _dotSize;
    private double _outlineThickness;
    private double _opacity;
    private double _offsetX;
    private double _offsetY;
    private CrosshairShape _shape;
    private bool _outlineEnabled;
    private bool _isEnabled;
    private bool _startEnabledOnLaunch;
    private bool _showEditorGuides;
    private bool _livePreviewEnabled;
    private string? _editorImagePath;
    private double _editorImageX;
    private double _editorImageY;
    private List<EditorStroke> _editorStrokes = new();

    private string _editColorHex = "#FFFFFF";
    private string _editOutlineColorHex = "#000000";
    private double _editLineLength;
    private double _editThickness;
    private double _editGap;
    private double _editDotSize;
    private double _editOutlineThickness;
    private double _editOpacity;
    private double _editOffsetX;
    private double _editOffsetY;
    private CrosshairShape _editShape;
    private bool _editOutlineEnabled;
    private List<EditorStroke> _editStrokes = new();

    private bool _startInTray;
    private bool _startWithWindows;
    private WinForms.Keys _toggleHotkey;
    private WinForms.Keys _cycleHotkey;
    private bool _isUpdateAvailable;
    private string? _latestReleaseTag;
    private string? _latestInstallerUrl;

    public OverlayViewModel(ProfileStore store, ProfileStorage storage)
    {
        _store = store;
        _storage = storage;

        Profiles = new ObservableCollection<string>(_store.Profiles.Select(p => p.Name));
        Presets = new ObservableCollection<CrosshairPreset>(BuildPresets());
        _selectedProfileName = string.IsNullOrWhiteSpace(store.CurrentProfile)
            ? Profiles.FirstOrDefault() ?? "Default"
            : store.CurrentProfile;

        CrosshairSettings initial = GetProfileSettings(_selectedProfileName) ?? new CrosshairSettings();
        ApplySettings(initial);
        ApplyEditorSettingsFromProfile(initial);

        IsEnabled = StartEnabledOnLaunch;

        _crosshairBrush = BuildBrush(_colorHex);
        _editCrosshairBrush = BuildBrush(_editColorHex);
        _outlineBrush = BuildBrush(_outlineColorHex);
        _editOutlineBrush = BuildBrush(_editOutlineColorHex);

        _startInTray = _store.StartInTray;
        _startWithWindows = _store.StartWithWindows;
        _toggleHotkey = (WinForms.Keys)_store.ToggleHotkey;
        _cycleHotkey = (WinForms.Keys)_store.CycleHotkey;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<string> Profiles { get; }
    public ObservableCollection<CrosshairPreset> Presets { get; }

    public string SelectedProfileName
    {
        get => _selectedProfileName;
        set
        {
            if (SetField(ref _selectedProfileName, value))
            {
                _store.CurrentProfile = value;
            }
        }
    }

    public string NewProfileName
    {
        get => _newProfileName;
        set => SetField(ref _newProfileName, value);
    }

    public string RenameProfileName
    {
        get => _renameProfileName;
        set => SetField(ref _renameProfileName, value);
    }

    public double ViewportWidth
    {
        get => _viewportWidth;
        set
        {
            if (SetField(ref _viewportWidth, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set
        {
            if (SetField(ref _viewportHeight, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public string ColorHex
    {
        get => _colorHex;
        set
        {
            if (SetField(ref _colorHex, value))
            {
                _crosshairBrush = BuildBrush(value);
                OnPropertyChanged(nameof(CrosshairBrush));
            }
        }
    }

    public SolidColorBrush CrosshairBrush => _crosshairBrush;

    public string EditColorHex
    {
        get => _editColorHex;
        set
        {
            if (SetField(ref _editColorHex, value))
            {
                _editCrosshairBrush = BuildBrush(value);
                OnPropertyChanged(nameof(EditCrosshairBrush));
                ApplyEditorSettingsIfLive();
            }
        }
    }

    public SolidColorBrush EditCrosshairBrush => _editCrosshairBrush;

    public string OutlineColorHex
    {
        get => _outlineColorHex;
        set
        {
            if (SetField(ref _outlineColorHex, value))
            {
                _outlineBrush = BuildBrush(value);
                OnPropertyChanged(nameof(OutlineBrush));
            }
        }
    }

    public SolidColorBrush OutlineBrush => _outlineBrush;

    public string EditOutlineColorHex
    {
        get => _editOutlineColorHex;
        set
        {
            if (SetField(ref _editOutlineColorHex, value))
            {
                _editOutlineBrush = BuildBrush(value);
                OnPropertyChanged(nameof(EditOutlineBrush));
                ApplyEditorSettingsIfLive();
            }
        }
    }

    public SolidColorBrush EditOutlineBrush => _editOutlineBrush;

    public double LineLength
    {
        get => _lineLength;
        set
        {
            if (SetField(ref _lineLength, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double EditLineLength
    {
        get => _editLineLength;
        set
        {
            if (SetField(ref _editLineLength, value))
            {
                ApplyEditorSettingsIfLive();
                OnPropertyChanged(nameof(EditLeftX));
                OnPropertyChanged(nameof(EditRightX));
                OnPropertyChanged(nameof(EditTopY));
                OnPropertyChanged(nameof(EditBottomY));
            }
        }
    }

    public double Thickness
    {
        get => _thickness;
        set
        {
            if (SetField(ref _thickness, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double EditThickness
    {
        get => _editThickness;
        set
        {
            if (SetField(ref _editThickness, value))
            {
                ApplyEditorSettingsIfLive();
                OnPropertyChanged(nameof(EditLeftY));
                OnPropertyChanged(nameof(EditRightY));
                OnPropertyChanged(nameof(EditTopX));
                OnPropertyChanged(nameof(EditBottomX));
            }
        }
    }

    public double Gap
    {
        get => _gap;
        set
        {
            if (SetField(ref _gap, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double EditGap
    {
        get => _editGap;
        set
        {
            if (SetField(ref _editGap, value))
            {
                ApplyEditorSettingsIfLive();
                OnPropertyChanged(nameof(EditLeftX));
                OnPropertyChanged(nameof(EditRightX));
                OnPropertyChanged(nameof(EditTopY));
                OnPropertyChanged(nameof(EditBottomY));
            }
        }
    }

    public double DotSize
    {
        get => _dotSize;
        set
        {
            if (SetField(ref _dotSize, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double EditDotSize
    {
        get => _editDotSize;
        set
        {
            if (SetField(ref _editDotSize, value))
            {
                ApplyEditorSettingsIfLive();
                OnPropertyChanged(nameof(EditDotX));
                OnPropertyChanged(nameof(EditDotY));
            }
        }
    }

    public double OutlineThickness
    {
        get => _outlineThickness;
        set
        {
            if (SetField(ref _outlineThickness, value))
            {
                NotifyOutlineGeometryChanged();
            }
        }
    }

    public double EditOutlineThickness
    {
        get => _editOutlineThickness;
        set
        {
            if (SetField(ref _editOutlineThickness, value))
            {
                NotifyEditOutlineGeometryChanged();
                ApplyEditorSettingsIfLive();
            }
        }
    }

    public double Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, value);
    }

    public double EditOpacity
    {
        get => _editOpacity;
        set
        {
            if (SetField(ref _editOpacity, value))
            {
                ApplyEditorSettingsIfLive();
            }
        }
    }

    public double OffsetX
    {
        get => _offsetX;
        set
        {
            if (SetField(ref _offsetX, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double EditOffsetX
    {
        get => _editOffsetX;
        set
        {
            if (SetField(ref _editOffsetX, value))
            {
                ApplyEditorSettingsIfLive();
                NotifyEditorGeometryChanged();
            }
        }
    }

    public double OffsetY
    {
        get => _offsetY;
        set
        {
            if (SetField(ref _offsetY, value))
            {
                NotifyCrosshairGeometryChanged();
            }
        }
    }

    public double EditOffsetY
    {
        get => _editOffsetY;
        set
        {
            if (SetField(ref _editOffsetY, value))
            {
                ApplyEditorSettingsIfLive();
                NotifyEditorGeometryChanged();
            }
        }
    }

    public CrosshairShape Shape
    {
        get => _shape;
        set
        {
            if (SetField(ref _shape, value))
            {
                NotifyShapeChanged();
            }
        }
    }

    public CrosshairShape EditShape
    {
        get => _editShape;
        set
        {
            if (SetField(ref _editShape, value))
            {
                NotifyEditShapeChanged();
                ApplyEditorSettingsIfLive();
            }
        }
    }

    public bool OutlineEnabled
    {
        get => _outlineEnabled;
        set
        {
            if (SetField(ref _outlineEnabled, value))
            {
                OnPropertyChanged(nameof(OutlineVisibility));
                OnPropertyChanged(nameof(ShowOutlineLeftArm));
                OnPropertyChanged(nameof(ShowOutlineRightArm));
                OnPropertyChanged(nameof(ShowOutlineTopArm));
                OnPropertyChanged(nameof(ShowOutlineBottomArm));
                OnPropertyChanged(nameof(ShowOutlineDot));
                OnPropertyChanged(nameof(ShowOutlineCircle));
                OnPropertyChanged(nameof(ShowOutlineX));
            }
        }
    }

    public bool EditOutlineEnabled
    {
        get => _editOutlineEnabled;
        set
        {
            if (SetField(ref _editOutlineEnabled, value))
            {
                OnPropertyChanged(nameof(EditOutlineVisibility));
                OnPropertyChanged(nameof(ShowEditOutlineLeftArm));
                OnPropertyChanged(nameof(ShowEditOutlineRightArm));
                OnPropertyChanged(nameof(ShowEditOutlineTopArm));
                OnPropertyChanged(nameof(ShowEditOutlineBottomArm));
                OnPropertyChanged(nameof(ShowEditOutlineDot));
                OnPropertyChanged(nameof(ShowEditOutlineCircle));
                OnPropertyChanged(nameof(ShowEditOutlineX));
                ApplyEditorSettingsIfLive();
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public bool StartEnabledOnLaunch
    {
        get => _startEnabledOnLaunch;
        set => SetField(ref _startEnabledOnLaunch, value);
    }

    public bool ShowEditorGuides
    {
        get => _showEditorGuides;
        set => SetField(ref _showEditorGuides, value);
    }

    public bool LivePreviewEnabled
    {
        get => _livePreviewEnabled;
        set
        {
            if (SetField(ref _livePreviewEnabled, value) && _livePreviewEnabled)
            {
                ApplyEditorSettings();
            }
        }
    }

    public string? EditorImagePath
    {
        get => _editorImagePath;
        set => SetField(ref _editorImagePath, value);
    }

    public double EditorImageX
    {
        get => _editorImageX;
        set => SetField(ref _editorImageX, value);
    }

    public double EditorImageY
    {
        get => _editorImageY;
        set => SetField(ref _editorImageY, value);
    }

    public List<EditorStroke> EditorStrokes
    {
        get => _editorStrokes;
        set => SetField(ref _editorStrokes, value);
    }

    public List<EditorStroke> EditStrokes
    {
        get => _editStrokes;
        set => SetField(ref _editStrokes, value);
    }

    public bool StartInTray
    {
        get => _startInTray;
        set
        {
            if (SetField(ref _startInTray, value))
            {
                _store.StartInTray = value;
            }
        }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetField(ref _startWithWindows, value))
            {
                _store.StartWithWindows = value;
            }
        }
    }

    public WinForms.Keys ToggleHotkey
    {
        get => _toggleHotkey;
        set
        {
            if (SetField(ref _toggleHotkey, value))
            {
                _store.ToggleHotkey = (int)value;
            }
        }
    }

    public WinForms.Keys CycleHotkey
    {
        get => _cycleHotkey;
        set
        {
            if (SetField(ref _cycleHotkey, value))
            {
                _store.CycleHotkey = (int)value;
            }
        }
    }

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        set => SetField(ref _isUpdateAvailable, value);
    }

    public string? LatestReleaseTag
    {
        get => _latestReleaseTag;
        set => SetField(ref _latestReleaseTag, value);
    }

    public string? LatestInstallerUrl
    {
        get => _latestInstallerUrl;
        set => SetField(ref _latestInstallerUrl, value);
    }

    public double LeftX => CenterX - Gap - LineLength;
    public double LeftY => CenterY - Thickness / 2;
    public double RightX => CenterX + Gap;
    public double RightY => CenterY - Thickness / 2;
    public double TopX => CenterX - Thickness / 2;
    public double TopY => CenterY - Gap - LineLength;
    public double BottomX => CenterX - Thickness / 2;
    public double BottomY => CenterY + Gap;
    public double DotX => CenterX - DotSize / 2;
    public double DotY => CenterY - DotSize / 2;

    public double OutlineLeftX => CenterX - Gap - OutlineLineLength;
    public double OutlineLeftY => CenterY - OutlineThicknessValue / 2;
    public double OutlineRightX => CenterX + Gap;
    public double OutlineRightY => CenterY - OutlineThicknessValue / 2;
    public double OutlineTopX => CenterX - OutlineThicknessValue / 2;
    public double OutlineTopY => CenterY - Gap - OutlineLineLength;
    public double OutlineBottomX => CenterX - OutlineThicknessValue / 2;
    public double OutlineBottomY => CenterY + Gap;
    public double OutlineDotX => CenterX - OutlineDotSize / 2;
    public double OutlineDotY => CenterY - OutlineDotSize / 2;

    public double CircleDiameter => LineLength * 2;
    public double CircleX => CenterX - LineLength;
    public double CircleY => CenterY - LineLength;
    public double OutlineCircleDiameter => CircleDiameter + OutlineThickness * 2;
    public double OutlineCircleX => CircleX - OutlineThickness;
    public double OutlineCircleY => CircleY - OutlineThickness;
    public double OutlineCircleStroke => Thickness + OutlineThickness * 2;

    public double XDiag1X1 => CenterX - LineLength / 2;
    public double XDiag1Y1 => CenterY - LineLength / 2;
    public double XDiag1X2 => CenterX + LineLength / 2;
    public double XDiag1Y2 => CenterY + LineLength / 2;
    public double XDiag2X1 => CenterX - LineLength / 2;
    public double XDiag2Y1 => CenterY + LineLength / 2;
    public double XDiag2X2 => CenterX + LineLength / 2;
    public double XDiag2Y2 => CenterY - LineLength / 2;

    public double OutlineXDiag1X1 => CenterX - OutlineLineLength / 2;
    public double OutlineXDiag1Y1 => CenterY - OutlineLineLength / 2;
    public double OutlineXDiag1X2 => CenterX + OutlineLineLength / 2;
    public double OutlineXDiag1Y2 => CenterY + OutlineLineLength / 2;
    public double OutlineXDiag2X1 => CenterX - OutlineLineLength / 2;
    public double OutlineXDiag2Y1 => CenterY + OutlineLineLength / 2;
    public double OutlineXDiag2X2 => CenterX + OutlineLineLength / 2;
    public double OutlineXDiag2Y2 => CenterY - OutlineLineLength / 2;

    private double CenterX => ViewportWidth / 2 + OffsetX;
    private double CenterY => ViewportHeight / 2 + OffsetY;
    private double EditCenterX => ViewportWidth / 2 + EditOffsetX;
    private double EditCenterY => ViewportHeight / 2 + EditOffsetY;

    public double EditLeftX => EditCenterX - EditGap - EditLineLength;
    public double EditLeftY => EditCenterY - EditThickness / 2;
    public double EditRightX => EditCenterX + EditGap;
    public double EditRightY => EditCenterY - EditThickness / 2;
    public double EditTopX => EditCenterX - EditThickness / 2;
    public double EditTopY => EditCenterY - EditGap - EditLineLength;
    public double EditBottomX => EditCenterX - EditThickness / 2;
    public double EditBottomY => EditCenterY + EditGap;

    public double EditDotX => EditCenterX - EditDotSize / 2;
    public double EditDotY => EditCenterY - EditDotSize / 2;

    public double EditOutlineLeftX => EditCenterX - EditGap - EditOutlineLineLength;
    public double EditOutlineLeftY => EditCenterY - EditOutlineThicknessValue / 2;
    public double EditOutlineRightX => EditCenterX + EditGap;
    public double EditOutlineRightY => EditCenterY - EditOutlineThicknessValue / 2;
    public double EditOutlineTopX => EditCenterX - EditOutlineThicknessValue / 2;
    public double EditOutlineTopY => EditCenterY - EditGap - EditOutlineLineLength;
    public double EditOutlineBottomX => EditCenterX - EditOutlineThicknessValue / 2;
    public double EditOutlineBottomY => EditCenterY + EditGap;
    public double EditOutlineDotX => EditCenterX - EditOutlineDotSize / 2;
    public double EditOutlineDotY => EditCenterY - EditOutlineDotSize / 2;

    public double EditCircleDiameter => EditLineLength * 2;
    public double EditCircleX => EditCenterX - EditLineLength;
    public double EditCircleY => EditCenterY - EditLineLength;
    public double EditOutlineCircleDiameter => EditCircleDiameter + EditOutlineThickness * 2;
    public double EditOutlineCircleX => EditCircleX - EditOutlineThickness;
    public double EditOutlineCircleY => EditCircleY - EditOutlineThickness;
    public double EditOutlineCircleStroke => EditThickness + EditOutlineThickness * 2;

    public double EditXDiag1X1 => EditCenterX - EditLineLength / 2;
    public double EditXDiag1Y1 => EditCenterY - EditLineLength / 2;
    public double EditXDiag1X2 => EditCenterX + EditLineLength / 2;
    public double EditXDiag1Y2 => EditCenterY + EditLineLength / 2;
    public double EditXDiag2X1 => EditCenterX - EditLineLength / 2;
    public double EditXDiag2Y1 => EditCenterY + EditLineLength / 2;
    public double EditXDiag2X2 => EditCenterX + EditLineLength / 2;
    public double EditXDiag2Y2 => EditCenterY - EditLineLength / 2;

    public double EditOutlineXDiag1X1 => EditCenterX - EditOutlineLineLength / 2;
    public double EditOutlineXDiag1Y1 => EditCenterY - EditOutlineLineLength / 2;
    public double EditOutlineXDiag1X2 => EditCenterX + EditOutlineLineLength / 2;
    public double EditOutlineXDiag1Y2 => EditCenterY + EditOutlineLineLength / 2;
    public double EditOutlineXDiag2X1 => EditCenterX - EditOutlineLineLength / 2;
    public double EditOutlineXDiag2Y1 => EditCenterY + EditOutlineLineLength / 2;
    public double EditOutlineXDiag2X2 => EditCenterX + EditOutlineLineLength / 2;
    public double EditOutlineXDiag2Y2 => EditCenterY - EditOutlineLineLength / 2;

    public double OutlineLineLength => LineLength + OutlineThickness * 2;
    public double OutlineThicknessValue => Thickness + OutlineThickness * 2;
    public double OutlineDotSize => DotSize + OutlineThickness * 2;
    public double EditOutlineLineLength => EditLineLength + EditOutlineThickness * 2;
    public double EditOutlineThicknessValue => EditThickness + EditOutlineThickness * 2;
    public double EditOutlineDotSize => EditDotSize + EditOutlineThickness * 2;

    public Visibility OutlineVisibility => OutlineEnabled ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EditOutlineVisibility => EditOutlineEnabled ? Visibility.Visible : Visibility.Collapsed;

    public bool ShowLeftArm => Shape is CrosshairShape.Cross or CrosshairShape.T;
    public bool ShowRightArm => Shape is CrosshairShape.Cross or CrosshairShape.T;
    public bool ShowTopArm => Shape is CrosshairShape.Cross or CrosshairShape.T;
    public bool ShowBottomArm => Shape == CrosshairShape.Cross;
    public bool ShowDot => Shape == CrosshairShape.Dot || (Shape == CrosshairShape.Cross && DotSize > 0);
    public bool ShowCircle => Shape == CrosshairShape.Circle;
    public bool ShowX => Shape == CrosshairShape.X;

    public bool ShowOutlineLeftArm => OutlineEnabled && ShowLeftArm;
    public bool ShowOutlineRightArm => OutlineEnabled && ShowRightArm;
    public bool ShowOutlineTopArm => OutlineEnabled && ShowTopArm;
    public bool ShowOutlineBottomArm => OutlineEnabled && ShowBottomArm;
    public bool ShowOutlineDot => OutlineEnabled && (Shape == CrosshairShape.Dot || (Shape == CrosshairShape.Cross && DotSize > 0));
    public bool ShowOutlineCircle => OutlineEnabled && Shape == CrosshairShape.Circle;
    public bool ShowOutlineX => OutlineEnabled && Shape == CrosshairShape.X;

    public bool ShowEditLeftArm => EditShape is CrosshairShape.Cross or CrosshairShape.T;
    public bool ShowEditRightArm => EditShape is CrosshairShape.Cross or CrosshairShape.T;
    public bool ShowEditTopArm => EditShape is CrosshairShape.Cross or CrosshairShape.T;
    public bool ShowEditBottomArm => EditShape == CrosshairShape.Cross;
    public bool ShowEditDot => EditShape == CrosshairShape.Dot || (EditShape == CrosshairShape.Cross && EditDotSize > 0);
    public bool ShowEditCircle => EditShape == CrosshairShape.Circle;
    public bool ShowEditX => EditShape == CrosshairShape.X;

    public bool ShowEditOutlineLeftArm => EditOutlineEnabled && ShowEditLeftArm;
    public bool ShowEditOutlineRightArm => EditOutlineEnabled && ShowEditRightArm;
    public bool ShowEditOutlineTopArm => EditOutlineEnabled && ShowEditTopArm;
    public bool ShowEditOutlineBottomArm => EditOutlineEnabled && ShowEditBottomArm;
    public bool ShowEditOutlineDot => EditOutlineEnabled && (EditShape == CrosshairShape.Dot || (EditShape == CrosshairShape.Cross && EditDotSize > 0));
    public bool ShowEditOutlineCircle => EditOutlineEnabled && EditShape == CrosshairShape.Circle;
    public bool ShowEditOutlineX => EditOutlineEnabled && EditShape == CrosshairShape.X;

    public void AddProfile()
    {
        string name = NewProfileName.Trim();
        if (string.IsNullOrWhiteSpace(name) || Profiles.Contains(name))
        {
            return;
        }

        Profiles.Add(name);
        _store.Profiles.Add(new CrosshairProfile
        {
            Name = name,
            Settings = GetEditorSettingsSnapshot()
        });
        SelectedProfileName = name;
        NewProfileName = string.Empty;
        SaveCurrentProfile();
    }

    public void RenameSelectedProfile()
    {
        string newName = RenameProfileName.Trim();
        if (string.IsNullOrWhiteSpace(newName) || Profiles.Contains(newName))
        {
            return;
        }

        CrosshairProfile? profile = _store.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null)
        {
            return;
        }

        int index = Profiles.IndexOf(SelectedProfileName);
        profile.Name = newName;
        Profiles[index] = newName;
        SelectedProfileName = newName;
        RenameProfileName = string.Empty;
        SaveCurrentProfile();
    }

    public void DuplicateSelectedProfile()
    {
        string baseName = string.IsNullOrWhiteSpace(NewProfileName)
            ? SelectedProfileName + " Copy"
            : NewProfileName.Trim();
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return;
        }

        string uniqueName = EnsureUniqueName(baseName);
        CrosshairProfile? profile = _store.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null)
        {
            return;
        }

        var clone = new CrosshairProfile
        {
            Name = uniqueName,
            Settings = GetEditorSettingsSnapshot()
        };

        _store.Profiles.Add(clone);
        Profiles.Add(uniqueName);
        SelectedProfileName = uniqueName;
        NewProfileName = string.Empty;
        SaveCurrentProfile();
    }

    public void ExportProfile(string filePath)
    {
        CrosshairProfile? profile = _store.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null)
        {
            return;
        }

        var exportProfile = new CrosshairProfile
        {
            Name = profile.Name,
            Settings = GetEditorSettingsSnapshot()
        };

        ProfilePackageService.ExportProfile(filePath, exportProfile);
    }

    public void ImportProfile(string filePath)
    {
        CrosshairProfile? profile = ProfilePackageService.ImportProfile(filePath);
        if (profile == null)
        {
            return;
        }

        profile.Name = EnsureUniqueName(profile.Name);
        _store.Profiles.Add(profile);
        Profiles.Add(profile.Name);
        SelectedProfileName = profile.Name;
        ApplyEditorSettingsFromProfile(profile.Settings);
    }

    public void SaveCurrentProfile()
    {
        CrosshairProfile? profile = _store.Profiles.FirstOrDefault(p => p.Name == SelectedProfileName);
        if (profile == null)
        {
            profile = new CrosshairProfile { Name = SelectedProfileName };
            _store.Profiles.Add(profile);
            if (!Profiles.Contains(SelectedProfileName))
            {
                Profiles.Add(SelectedProfileName);
            }
        }

        profile.Settings = GetEditorSettingsSnapshot();
        _store.CurrentProfile = SelectedProfileName;
        _storage.Save(_store);
    }

    public void LoadSelectedProfile()
    {
        CrosshairSettings? settings = GetProfileSettings(SelectedProfileName);
        if (settings == null)
        {
            return;
        }

        ApplyEditorSettingsFromProfile(settings);
        ApplyEditorSettingsIfLive();
    }

    public void ApplyProfileToOverlay(string profileName)
    {
        CrosshairSettings? settings = GetProfileSettings(profileName);
        if (settings == null)
        {
            return;
        }

        ApplySettings(settings);
        SelectedProfileName = profileName;
    }

    public void ApplyPresetToEditor(CrosshairPreset preset)
    {
        if (preset == null)
        {
            return;
        }

        ApplyEditorSettingsFromProfile(preset.Settings);
        ApplyEditorSettingsIfLive();
    }

    public void ApplyEditorSettings()
    {
        ColorHex = EditColorHex;
        OutlineColorHex = EditOutlineColorHex;
        LineLength = EditLineLength;
        Thickness = EditThickness;
        Gap = EditGap;
        DotSize = EditDotSize;
        OutlineThickness = EditOutlineThickness;
        Opacity = EditOpacity;
        OffsetX = EditOffsetX;
        OffsetY = EditOffsetY;
        Shape = EditShape;
        OutlineEnabled = EditOutlineEnabled;
        EditorStrokes = CloneStrokes(EditStrokes);
    }

    private CrosshairSettings? GetProfileSettings(string profileName)
    {
        return _store.Profiles.FirstOrDefault(p => p.Name == profileName)?.Settings;
    }

    private void ApplySettings(CrosshairSettings settings)
    {
        ColorHex = settings.ColorHex;
        OutlineColorHex = settings.OutlineColorHex;
        LineLength = settings.LineLength;
        Thickness = settings.Thickness;
        Gap = settings.Gap;
        DotSize = settings.DotSize;
        OutlineThickness = settings.OutlineThickness;
        Opacity = settings.Opacity;
        OffsetX = settings.OffsetX;
        OffsetY = settings.OffsetY;
        Shape = settings.Shape;
        IsEnabled = settings.IsEnabled;
        StartEnabledOnLaunch = settings.StartEnabledOnLaunch;
        OutlineEnabled = settings.OutlineEnabled;
        ShowEditorGuides = settings.ShowEditorGuides;
        LivePreviewEnabled = settings.LivePreviewEnabled;
        EditorImagePath = settings.EditorImagePath;
        EditorImageX = settings.EditorImageX;
        EditorImageY = settings.EditorImageY;
        EditorStrokes = CloneStrokes(settings.EditorStrokes);
    }

    private void ApplyEditorSettingsFromProfile(CrosshairSettings settings)
    {
        EditColorHex = settings.ColorHex;
        EditOutlineColorHex = settings.OutlineColorHex;
        EditLineLength = settings.LineLength;
        EditThickness = settings.Thickness;
        EditGap = settings.Gap;
        EditDotSize = settings.DotSize;
        EditOutlineThickness = settings.OutlineThickness;
        EditOpacity = settings.Opacity;
        EditOffsetX = settings.OffsetX;
        EditOffsetY = settings.OffsetY;
        EditShape = settings.Shape;
        EditOutlineEnabled = settings.OutlineEnabled;
        ShowEditorGuides = settings.ShowEditorGuides;
        LivePreviewEnabled = settings.LivePreviewEnabled;
        EditorImagePath = settings.EditorImagePath;
        EditorImageX = settings.EditorImageX;
        EditorImageY = settings.EditorImageY;
        EditStrokes = CloneStrokes(settings.EditorStrokes);
        OnPropertyChanged(nameof(EditCrosshairBrush));
        OnPropertyChanged(nameof(EditOutlineBrush));
    }

    private CrosshairSettings GetSettingsSnapshot()
    {
        return new CrosshairSettings
        {
            ColorHex = ColorHex,
            OutlineColorHex = OutlineColorHex,
            LineLength = LineLength,
            Thickness = Thickness,
            Gap = Gap,
            DotSize = DotSize,
            OutlineThickness = OutlineThickness,
            Opacity = Opacity,
            OffsetX = OffsetX,
            OffsetY = OffsetY,
            Shape = Shape,
            IsEnabled = IsEnabled,
            StartEnabledOnLaunch = StartEnabledOnLaunch,
            OutlineEnabled = OutlineEnabled,
            ShowEditorGuides = ShowEditorGuides,
            LivePreviewEnabled = LivePreviewEnabled,
            EditorImagePath = EditorImagePath,
            EditorImageX = EditorImageX,
            EditorImageY = EditorImageY,
            EditorStrokes = CloneStrokes(EditorStrokes)
        };
    }

    private CrosshairSettings GetEditorSettingsSnapshot()
    {
        return new CrosshairSettings
        {
            ColorHex = EditColorHex,
            OutlineColorHex = EditOutlineColorHex,
            LineLength = EditLineLength,
            Thickness = EditThickness,
            Gap = EditGap,
            DotSize = EditDotSize,
            OutlineThickness = EditOutlineThickness,
            Opacity = EditOpacity,
            OffsetX = EditOffsetX,
            OffsetY = EditOffsetY,
            Shape = EditShape,
            IsEnabled = IsEnabled,
            StartEnabledOnLaunch = StartEnabledOnLaunch,
            OutlineEnabled = EditOutlineEnabled,
            ShowEditorGuides = ShowEditorGuides,
            LivePreviewEnabled = LivePreviewEnabled,
            EditorImagePath = EditorImagePath,
            EditorImageX = EditorImageX,
            EditorImageY = EditorImageY,
            EditorStrokes = CloneStrokes(EditStrokes)
        };
    }

    private SolidColorBrush BuildBrush(string colorHex)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
            return new SolidColorBrush(color);
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }

    private void NotifyCrosshairGeometryChanged()
    {
        OnPropertyChanged(nameof(LeftX));
        OnPropertyChanged(nameof(LeftY));
        OnPropertyChanged(nameof(RightX));
        OnPropertyChanged(nameof(RightY));
        OnPropertyChanged(nameof(TopX));
        OnPropertyChanged(nameof(TopY));
        OnPropertyChanged(nameof(BottomX));
        OnPropertyChanged(nameof(BottomY));
        OnPropertyChanged(nameof(DotX));
        OnPropertyChanged(nameof(DotY));
        OnPropertyChanged(nameof(CircleX));
        OnPropertyChanged(nameof(CircleY));
        OnPropertyChanged(nameof(CircleDiameter));
        OnPropertyChanged(nameof(XDiag1X1));
        OnPropertyChanged(nameof(XDiag1Y1));
        OnPropertyChanged(nameof(XDiag1X2));
        OnPropertyChanged(nameof(XDiag1Y2));
        OnPropertyChanged(nameof(XDiag2X1));
        OnPropertyChanged(nameof(XDiag2Y1));
        OnPropertyChanged(nameof(XDiag2X2));
        OnPropertyChanged(nameof(XDiag2Y2));
        NotifyOutlineGeometryChanged();
        NotifyShapeChanged();
    }

    private void NotifyEditorGeometryChanged()
    {
        OnPropertyChanged(nameof(EditLeftX));
        OnPropertyChanged(nameof(EditLeftY));
        OnPropertyChanged(nameof(EditRightX));
        OnPropertyChanged(nameof(EditRightY));
        OnPropertyChanged(nameof(EditTopX));
        OnPropertyChanged(nameof(EditTopY));
        OnPropertyChanged(nameof(EditBottomX));
        OnPropertyChanged(nameof(EditBottomY));
        OnPropertyChanged(nameof(EditDotX));
        OnPropertyChanged(nameof(EditDotY));
        OnPropertyChanged(nameof(EditCircleX));
        OnPropertyChanged(nameof(EditCircleY));
        OnPropertyChanged(nameof(EditCircleDiameter));
        OnPropertyChanged(nameof(EditXDiag1X1));
        OnPropertyChanged(nameof(EditXDiag1Y1));
        OnPropertyChanged(nameof(EditXDiag1X2));
        OnPropertyChanged(nameof(EditXDiag1Y2));
        OnPropertyChanged(nameof(EditXDiag2X1));
        OnPropertyChanged(nameof(EditXDiag2Y1));
        OnPropertyChanged(nameof(EditXDiag2X2));
        OnPropertyChanged(nameof(EditXDiag2Y2));
        NotifyEditOutlineGeometryChanged();
        NotifyEditShapeChanged();
    }

    private void ApplyEditorSettingsIfLive()
    {
        if (LivePreviewEnabled)
        {
            ApplyEditorSettings();
        }
    }

    private void NotifyShapeChanged()
    {
        OnPropertyChanged(nameof(ShowLeftArm));
        OnPropertyChanged(nameof(ShowRightArm));
        OnPropertyChanged(nameof(ShowTopArm));
        OnPropertyChanged(nameof(ShowBottomArm));
        OnPropertyChanged(nameof(ShowDot));
        OnPropertyChanged(nameof(ShowCircle));
        OnPropertyChanged(nameof(ShowX));
        OnPropertyChanged(nameof(ShowOutlineLeftArm));
        OnPropertyChanged(nameof(ShowOutlineRightArm));
        OnPropertyChanged(nameof(ShowOutlineTopArm));
        OnPropertyChanged(nameof(ShowOutlineBottomArm));
        OnPropertyChanged(nameof(ShowOutlineDot));
        OnPropertyChanged(nameof(ShowOutlineCircle));
        OnPropertyChanged(nameof(ShowOutlineX));
    }

    private void NotifyEditShapeChanged()
    {
        OnPropertyChanged(nameof(ShowEditLeftArm));
        OnPropertyChanged(nameof(ShowEditRightArm));
        OnPropertyChanged(nameof(ShowEditTopArm));
        OnPropertyChanged(nameof(ShowEditBottomArm));
        OnPropertyChanged(nameof(ShowEditDot));
        OnPropertyChanged(nameof(ShowEditCircle));
        OnPropertyChanged(nameof(ShowEditX));
        OnPropertyChanged(nameof(ShowEditOutlineLeftArm));
        OnPropertyChanged(nameof(ShowEditOutlineRightArm));
        OnPropertyChanged(nameof(ShowEditOutlineTopArm));
        OnPropertyChanged(nameof(ShowEditOutlineBottomArm));
        OnPropertyChanged(nameof(ShowEditOutlineDot));
        OnPropertyChanged(nameof(ShowEditOutlineCircle));
        OnPropertyChanged(nameof(ShowEditOutlineX));
    }

    private void NotifyOutlineGeometryChanged()
    {
        OnPropertyChanged(nameof(OutlineLeftX));
        OnPropertyChanged(nameof(OutlineLeftY));
        OnPropertyChanged(nameof(OutlineRightX));
        OnPropertyChanged(nameof(OutlineRightY));
        OnPropertyChanged(nameof(OutlineTopX));
        OnPropertyChanged(nameof(OutlineTopY));
        OnPropertyChanged(nameof(OutlineBottomX));
        OnPropertyChanged(nameof(OutlineBottomY));
        OnPropertyChanged(nameof(OutlineDotX));
        OnPropertyChanged(nameof(OutlineDotY));
        OnPropertyChanged(nameof(OutlineCircleX));
        OnPropertyChanged(nameof(OutlineCircleY));
        OnPropertyChanged(nameof(OutlineCircleDiameter));
        OnPropertyChanged(nameof(OutlineXDiag1X1));
        OnPropertyChanged(nameof(OutlineXDiag1Y1));
        OnPropertyChanged(nameof(OutlineXDiag1X2));
        OnPropertyChanged(nameof(OutlineXDiag1Y2));
        OnPropertyChanged(nameof(OutlineXDiag2X1));
        OnPropertyChanged(nameof(OutlineXDiag2Y1));
        OnPropertyChanged(nameof(OutlineXDiag2X2));
        OnPropertyChanged(nameof(OutlineXDiag2Y2));
    }

    private void NotifyEditOutlineGeometryChanged()
    {
        OnPropertyChanged(nameof(EditOutlineLeftX));
        OnPropertyChanged(nameof(EditOutlineLeftY));
        OnPropertyChanged(nameof(EditOutlineRightX));
        OnPropertyChanged(nameof(EditOutlineRightY));
        OnPropertyChanged(nameof(EditOutlineTopX));
        OnPropertyChanged(nameof(EditOutlineTopY));
        OnPropertyChanged(nameof(EditOutlineBottomX));
        OnPropertyChanged(nameof(EditOutlineBottomY));
        OnPropertyChanged(nameof(EditOutlineDotX));
        OnPropertyChanged(nameof(EditOutlineDotY));
        OnPropertyChanged(nameof(EditOutlineCircleX));
        OnPropertyChanged(nameof(EditOutlineCircleY));
        OnPropertyChanged(nameof(EditOutlineCircleDiameter));
        OnPropertyChanged(nameof(EditOutlineXDiag1X1));
        OnPropertyChanged(nameof(EditOutlineXDiag1Y1));
        OnPropertyChanged(nameof(EditOutlineXDiag1X2));
        OnPropertyChanged(nameof(EditOutlineXDiag1Y2));
        OnPropertyChanged(nameof(EditOutlineXDiag2X1));
        OnPropertyChanged(nameof(EditOutlineXDiag2Y1));
        OnPropertyChanged(nameof(EditOutlineXDiag2X2));
        OnPropertyChanged(nameof(EditOutlineXDiag2Y2));
    }

    private static List<EditorStroke> CloneStrokes(IEnumerable<EditorStroke> strokes)
    {
        return strokes.Select(stroke => new EditorStroke
        {
            Points = stroke.Points.Select(point => new EditorPoint { X = point.X, Y = point.Y }).ToList()
        }).ToList();
    }

    private static IEnumerable<CrosshairPreset> BuildPresets()
    {
        return new List<CrosshairPreset>
        {
            new()
            {
                Name = "Thin Cross",
                Settings = new CrosshairSettings
                {
                    LineLength = 18,
                    Thickness = 2,
                    Gap = 4,
                    DotSize = 0,
                    Shape = CrosshairShape.Cross
                }
            },
            new()
            {
                Name = "Thick Cross + Gap",
                Settings = new CrosshairSettings
                {
                    LineLength = 16,
                    Thickness = 4,
                    Gap = 6,
                    DotSize = 0,
                    Shape = CrosshairShape.Cross
                }
            },
            new()
            {
                Name = "Cross + Dot",
                Settings = new CrosshairSettings
                {
                    LineLength = 16,
                    Thickness = 2,
                    Gap = 4,
                    DotSize = 4,
                    Shape = CrosshairShape.Cross
                }
            },
            new()
            {
                Name = "Dot",
                Settings = new CrosshairSettings
                {
                    DotSize = 6,
                    Shape = CrosshairShape.Dot
                }
            },
            new()
            {
                Name = "Circle",
                Settings = new CrosshairSettings
                {
                    LineLength = 10,
                    Thickness = 2,
                    Shape = CrosshairShape.Circle
                }
            },
            new()
            {
                Name = "X",
                Settings = new CrosshairSettings
                {
                    LineLength = 18,
                    Thickness = 2,
                    Shape = CrosshairShape.X
                }
            },
            new()
            {
                Name = "T",
                Settings = new CrosshairSettings
                {
                    LineLength = 18,
                    Thickness = 2,
                    Gap = 4,
                    Shape = CrosshairShape.T
                }
            }
        };
    }

    private string EnsureUniqueName(string name)
    {
        string candidate = name;
        int index = 1;
        while (Profiles.Contains(candidate))
        {
            candidate = name + " " + index;
            index++;
        }

        return candidate;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
