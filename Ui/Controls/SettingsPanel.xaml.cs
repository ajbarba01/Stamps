using System.Windows;
using System.Windows.Controls;
using Stamps.Core;

namespace Stamps.Ui.Controls;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel(
        IReadOnlyList<SettingDescriptor> descriptors,
        SettingsValues values,
        Action<string, object?> onChanged)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(onChanged);

        InitializeComponent();
        BuildRows(descriptors, values, onChanged);
    }

    private void BuildRows(
        IReadOnlyList<SettingDescriptor> descriptors,
        SettingsValues values,
        Action<string, object?> onChanged)
    {
        if (descriptors.Count == 0)
        {
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var empty = new TextBlock
            {
                Text = "No configurable settings.",
                Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Secondary"),
                Margin = new Thickness(0, 4, 0, 4),
            };
            Grid.SetRow(empty, 0);
            RootGrid.Children.Add(empty);
            return;
        }

        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < descriptors.Count; i++)
        {
            var d = descriptors[i];
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var labelStack = new StackPanel { Margin = new Thickness(0, 8, 16, 8) };
            labelStack.Children.Add(new TextBlock
            {
                Text = d.Label,
                Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Primary"),
            });
            if (!string.IsNullOrEmpty(d.Description))
            {
                labelStack.Children.Add(new TextBlock
                {
                    Text = d.Description,
                    Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Secondary"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 180,
                    Margin = new Thickness(0, 2, 0, 0),
                });
            }
            Grid.SetRow(labelStack, i); Grid.SetColumn(labelStack, 0);
            RootGrid.Children.Add(labelStack);

            var editor = BuildEditor(d, values, onChanged);
            editor.Margin = new Thickness(0, 8, 0, 8);
            Grid.SetRow(editor, i); Grid.SetColumn(editor, 1);
            RootGrid.Children.Add(editor);
        }
    }

    private FrameworkElement BuildEditor(
        SettingDescriptor d,
        SettingsValues values,
        Action<string, object?> onChanged)
    {
        switch (d)
        {
            case ToggleSetting toggle:
            {
                var cb = new CheckBox { IsChecked = values.GetBool(toggle.Key, toggle.Default) };
                cb.Checked   += (_, _) => onChanged(toggle.Key, true);
                cb.Unchecked += (_, _) => onChanged(toggle.Key, false);
                return cb;
            }
            case TextSetting text:
            {
                var tb = new TextBox
                {
                    Width = 320,
                    Text = values.GetString(text.Key, text.Default),
                };
                tb.TextChanged += (_, _) => onChanged(text.Key, tb.Text);
                return tb;
            }
            case NumberSetting num:
            {
                var nud = new Wpf.Ui.Controls.NumberBox
                {
                    Minimum = num.Min,
                    Maximum = num.Max,
                    Value = values.GetNumber(num.Key, num.Default),
                    Width = 120,
                };
                nud.ValueChanged += (_, _) => onChanged(num.Key, nud.Value);
                return nud;
            }
            case DropdownSetting drop:
            {
                var combo = new ComboBox { Width = 240 };
                foreach (var opt in drop.Options) combo.Items.Add(opt);
                var cur = values.GetString(drop.Key, drop.Default);
                combo.SelectedIndex = Math.Max(0, combo.Items.IndexOf(cur));
                combo.SelectionChanged += (_, _) =>
                    onChanged(drop.Key, combo.SelectedItem?.ToString() ?? "");
                return combo;
            }
            case FilePathSetting fp:
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal };
                var tb = new TextBox { Width = 280, Text = values.GetString(fp.Key, fp.Default) };
                tb.TextChanged += (_, _) => onChanged(fp.Key, tb.Text);
                var browse = new Button { Content = "Browse…", Margin = new Thickness(8, 0, 0, 0) };
                browse.Click += (_, _) =>
                {
                    var dlg = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = fp.Filter ?? "All files (*.*)|*.*",
                        FileName = tb.Text,
                    };
                    if (dlg.ShowDialog() == true) tb.Text = dlg.FileName;
                };
                panel.Children.Add(tb);
                panel.Children.Add(browse);
                return panel;
            }
            case HotkeySetting hk:
            {
                var box = new HotkeyCaptureBox
                {
                    Width = 240,
                    Hotkey = values.GetHotkey(hk.Key) ?? hk.Default,
                };
                box.HotkeyChanged += (_, _) => onChanged(hk.Key, box.Hotkey?.ToString());
                return box;
            }
            default:
                return new TextBlock
                {
                    Text = $"(unsupported: {d.GetType().Name})",
                    Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Secondary"),
                };
        }
    }
}
