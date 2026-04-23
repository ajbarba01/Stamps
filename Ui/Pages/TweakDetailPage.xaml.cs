using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Stamps.Core;
using Stamps.Ui.Controls;

namespace Stamps.Ui.Pages;

public partial class TweakDetailPage : UserControl, IPage, IDisposable
{
    private readonly ITweak _tweak;
    private readonly Action _onBack;

    public string Title => _tweak.Title;

    public TweakDetailPage(ITweak tweak, Action onBack)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        ArgumentNullException.ThrowIfNull(onBack);

        _tweak = tweak;
        _onBack = onBack;

        InitializeComponent();

        DescriptionText.Text = tweak.Description;

        OverviewTab.Content = BuildOverviewContent();
        ActionsTab.Content = BuildActionsContent();
        SettingsTab.Content = BuildSettingsContent();
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
        var stack = new StackPanel { Margin = new Thickness(0, 8, 0, 16) };
        scroll.Content = stack;

        if (_tweak.BuiltInActions.Count == 0 && _tweak.UserActionFactory is null)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "This tweak exposes no actions.",
                Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Secondary"),
            });
            return scroll;
        }

        foreach (var action in _tweak.BuiltInActions)
            stack.Children.Add(BuildActionCard(action));

        if (_tweak.UserActionFactory is { } factory)
        {
            var addBtn = new Button
            {
                Content = factory.CreateButtonLabel,
                Margin = new Thickness(0, 12, 0, 0),
                IsEnabled = false,
            };
            stack.Children.Add(addBtn);
        }

        return scroll;
    }

    private UIElement BuildActionCard(IAction action)
    {
        var card = new Border
        {
            Background = (System.Windows.Media.Brush)FindResource("Stamps.Background.Card"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 0, 0, 8),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = action.Title,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(titleBlock, 0); Grid.SetRow(titleBlock, 0);
        grid.Children.Add(titleBlock);

        var hotkeyBox = new HotkeyCaptureBox { Width = 220, Hotkey = action.Hotkey, Margin = new Thickness(8, 0, 8, 0) };
        hotkeyBox.HotkeyChanged += (_, _) => { action.Hotkey = hotkeyBox.Hotkey; action.PersistSettings(); };
        Grid.SetColumn(hotkeyBox, 1); Grid.SetRow(hotkeyBox, 0);
        grid.Children.Add(hotkeyBox);

        var enabledBox = new CheckBox
        {
            Content = "Enabled",
            IsChecked = action.Enabled,
            VerticalAlignment = VerticalAlignment.Center,
        };
        enabledBox.Checked += (_, _) => { action.Enabled = true; action.PersistSettings(); };
        enabledBox.Unchecked += (_, _) => { action.Enabled = false; action.PersistSettings(); };
        Grid.SetColumn(enabledBox, 2); Grid.SetRow(enabledBox, 0);
        grid.Children.Add(enabledBox);

        if (action.Settings.Count > 0)
        {
            var panel = new SettingsPanel(action.Settings, action.Values, (key, value) =>
            {
                action.Values.Set(key, value);
                action.PersistSettings();
            });
            panel.Margin = new Thickness(0, 12, 0, 0);
            Grid.SetColumnSpan(panel, 3); Grid.SetRow(panel, 1);
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
