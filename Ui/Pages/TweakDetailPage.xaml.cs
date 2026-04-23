using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using Stamps.Core;
using Stamps.Ui.Controls;
using Stamps.Ui.Theme;

namespace Stamps.Ui.Pages;

public partial class TweakDetailPage : UserControl, IPage, IDisposable
{
    private readonly ITweak _tweak;
    private readonly Action _onBack;

    private StackPanel? _actionsStack;
    private Button?     _addButton;
    private IActionFactory? _activeFactory;

    public string Title => _tweak.Title;

    public TweakDetailPage(ITweak tweak, Action onBack)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        ArgumentNullException.ThrowIfNull(onBack);

        _tweak  = tweak;
        _onBack = onBack;

        InitializeComponent();

        DescriptionText.Text = tweak.Description;

        OverviewTab.Content  = BuildOverviewContent();
        ActionsTab.Content   = BuildActionsContent();
        SettingsTab.Content  = BuildSettingsContent();
    }

    private void OnBackClick(object sender, RoutedEventArgs e) => _onBack();

    private UIElement BuildOverviewContent()
    {
        var view = new MarkdownView();
        var readmePath = Path.Combine(
            AppContext.BaseDirectory, "Tweaks", _tweak.Id, "README.md");
        view.LoadFromFile(readmePath);
        return view;
    }

    private UIElement BuildActionsContent()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        _actionsStack = new StackPanel { Margin = new Thickness(0, 8, 0, 16) };
        scroll.Content = _actionsStack;

        var factory = _tweak.UserActionFactory;
        _activeFactory = factory;

        if (_tweak.BuiltInActions.Count == 0 && factory is null)
        {
            _actionsStack.Children.Add(new TextBlock
            {
                Text       = "This tweak exposes no actions.",
                Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Secondary"),
            });
            return scroll;
        }

        foreach (var action in _tweak.BuiltInActions)
            _actionsStack.Children.Add(BuildActionCard(action));

        if (factory is not null)
        {
            _addButton = new Button
            {
                Content = factory.CreateButtonLabel,
                Margin = new Thickness(0, 12, 0, 0),
                MinWidth = 150,
                MinHeight = 35
            };
            _addButton.Click += OnAddActionClick;
            _actionsStack.Children.Add(_addButton);
        }

        return scroll;
    }

    private void OnAddActionClick(object sender, RoutedEventArgs e)
    {
        if (_activeFactory is not { } factory) return;

        var values = ShowCreateDialog(factory);
        if (values is null) return;

        var action = factory.Create(values);
        var card   = BuildActionCard(action);

        var insertIdx = _actionsStack!.Children.IndexOf(_addButton!);
        _actionsStack.Children.Insert(insertIdx, card);
    }

    private SettingsValues? ShowCreateDialog(IActionFactory factory)
    {
        var values       = new SettingsValues();
        var settingsPanel = new SettingsPanel(
            factory.TemplateSettings, values, (k, v) => values.Set(k, v));

        var okBtn = new Button
        {
            Content   = "Add",
            IsDefault = true,
            MinWidth  = 80,
        };
        var cancelBtn = new Button
        {
            Content  = "Cancel",
            IsCancel = true,
            MinWidth = 80,
            Margin   = new Thickness(8, 0, 0, 0),
        };

        var btnRow = new StackPanel
        {
            Orientation         = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin              = new Thickness(0, 20, 0, 0),
        };
        btnRow.Children.Add(okBtn);
        btnRow.Children.Add(cancelBtn);

        var content = new StackPanel();
        content.Children.Add(settingsPanel);
        content.Children.Add(btnRow);

        var dialog = new Window
        {
            Title                   = factory.CreateButtonLabel,
            Content                 = new Border { Padding = new Thickness(24), Child = content },
            Width                   = 460,
            SizeToContent           = SizeToContent.Height,
            WindowStartupLocation   = WindowStartupLocation.CenterOwner,
            Owner                   = Window.GetWindow(this),
            ResizeMode              = ResizeMode.NoResize,
            ShowInTaskbar           = false,
        };
        dialog.SourceInitialized += (_, _) =>
            ThemeService.ApplyToHwnd(new WindowInteropHelper(dialog).Handle);

        okBtn.Click += (_, _) => dialog.DialogResult = true;

        return dialog.ShowDialog() == true ? values : null;
    }

    private UIElement BuildActionCard(IAction action)
    {
        var card = new Border
        {
            Background   = (System.Windows.Media.Brush)FindResource("Stamps.Background.Card"),
            CornerRadius = new CornerRadius(8),
            Padding      = new Thickness(16, 12, 16, 12),
            Margin       = new Thickness(0, 0, 0, 8),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // delete btn column
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text                = action.Title,
            FontWeight          = FontWeights.SemiBold,
            VerticalAlignment   = VerticalAlignment.Center,
        };
        Grid.SetColumn(titleBlock, 0); Grid.SetRow(titleBlock, 0);
        grid.Children.Add(titleBlock);

        var hotkeyBox = new HotkeyCaptureBox { Width = 220, Hotkey = action.Hotkey, Margin = new Thickness(8, 0, 8, 0) };
        hotkeyBox.HotkeyChanged += (_, _) => { action.Hotkey = hotkeyBox.Hotkey; action.PersistSettings(); };
        Grid.SetColumn(hotkeyBox, 1); Grid.SetRow(hotkeyBox, 0);
        grid.Children.Add(hotkeyBox);

        var enabledBox = new CheckBox
        {
            Content           = "Enabled",
            IsChecked         = action.Enabled,
            VerticalAlignment = VerticalAlignment.Center,
        };
        enabledBox.Checked   += (_, _) => { action.Enabled = true;  action.PersistSettings(); };
        enabledBox.Unchecked += (_, _) => { action.Enabled = false; action.PersistSettings(); };
        Grid.SetColumn(enabledBox, 2); Grid.SetRow(enabledBox, 0);
        grid.Children.Add(enabledBox);

        if (action.IsUserCreated && _activeFactory is { } factory)
        {
            var deleteBtn = new Button
            {
                Content           = "Remove",
                Margin            = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            deleteBtn.Click += (_, _) =>
            {
                factory.Delete(action);
                _actionsStack?.Children.Remove(card);
            };
            Grid.SetColumn(deleteBtn, 3); Grid.SetRow(deleteBtn, 0);
            grid.Children.Add(deleteBtn);
        }

        if (action.Settings.Count > 0)
        {
            var panel = new SettingsPanel(action.Settings, action.Values, (key, value) =>
            {
                action.Values.Set(key, value);
                action.PersistSettings();
            });
            panel.Margin = new Thickness(0, 12, 0, 0);
            Grid.SetColumnSpan(panel, 4); Grid.SetRow(panel, 1);
            grid.Children.Add(panel);
        }

        card.Child = grid;
        return card;
    }

    private UIElement BuildSettingsContent()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(0, 8, 0, 16),
        };

        var custom = _tweak.CreateCustomSettingsControl();
        if (custom is UIElement customUi)
        {
            scroll.Content = customUi;
            return scroll;
        }

        var panel = new SettingsPanel(_tweak.Settings, _tweak.Values, (key, value) =>
        {
            _tweak.Values.Set(key, value);
            _tweak.PersistSettings();
        });
        scroll.Content = panel;
        return scroll;
    }

    public void Dispose() { }
}
