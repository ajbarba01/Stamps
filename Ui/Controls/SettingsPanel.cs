using System.Windows.Forms;
using Stamps.Core;

namespace Stamps.Ui.Controls;

/// <summary>
/// Renders a list of <see cref="SettingDescriptor"/>s as a vertical form, two columns wide
/// (label on the left, editor on the right), and notifies the host on every change.
/// </summary>
/// <remarks>
/// <para>The panel is purely a view: it does not persist values. The owning page passes the
/// current values in via the constructor and supplies an <see cref="Action{TKey, TValue}"/>
/// callback that fires on every edit. The page is responsible for batching and persisting
/// (typically by writing through a <see cref="Core.Services.ISettingsScope"/>).</para>
/// <para>Unrecognised descriptor types render a stub label rather than throwing — adding
/// new editor types should be done by extending the switch in <c>BuildEditor</c>.</para>
/// </remarks>
internal sealed class SettingsPanel : TableLayoutPanel
{
    private readonly IReadOnlyList<SettingDescriptor> _descriptors;
    private readonly SettingsValues _values;
    private readonly Action<string, object?> _onChanged;

    public SettingsPanel(
        IReadOnlyList<SettingDescriptor> descriptors,
        SettingsValues values,
        Action<string, object?> onChanged)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(onChanged);

        _descriptors = descriptors;
        _values = values;
        _onChanged = onChanged;

        ColumnCount = 2;
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.Transparent;
        Padding = new Padding(0, 4, 0, 4);

        BuildRows();
    }

    private void BuildRows()
    {
        if (_descriptors.Count == 0)
        {
            RowCount = 1;
            Controls.Add(new Label
            {
                Text = "No configurable settings.",
                ForeColor = Theme.SecondaryText,
                Font = Theme.Body,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4),
            }, column: 0, row: 0);
            SetColumnSpan(GetControlFromPosition(0, 0)!, 2);
            return;
        }

        for (int i = 0; i < _descriptors.Count; i++)
        {
            var d = _descriptors[i];
            RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var labelStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 8, 16, 8),
                BackColor = Color.Transparent,
            };
            labelStack.Controls.Add(new Label
            {
                Text = d.Label,
                Font = Theme.Body,
                AutoSize = true,
                Margin = new Padding(0),
            });
            if (!string.IsNullOrEmpty(d.Description))
            {
                labelStack.Controls.Add(new Label
                {
                    Text = d.Description,
                    Font = Theme.Body,
                    ForeColor = Theme.SecondaryText,
                    AutoSize = true,
                    MaximumSize = new Size(180, 0),
                    Margin = new Padding(0, 2, 0, 0),
                });
            }
            Controls.Add(labelStack, column: 0, row: i);

            var editor = BuildEditor(d);
            editor.Margin = new Padding(0, 8, 0, 8);
            Controls.Add(editor, column: 1, row: i);
        }
    }

    private Control BuildEditor(SettingDescriptor d)
    {
        switch (d)
        {
            case ToggleSetting toggle:
            {
                var cb = new CheckBox
                {
                    AutoSize = true,
                    Checked = _values.GetBool(toggle.Key, toggle.Default),
                    Font = Theme.Body,
                };
                cb.CheckedChanged += (_, _) => _onChanged(toggle.Key, cb.Checked);
                return cb;
            }
            case TextSetting text:
            {
                var tb = new TextBox
                {
                    Width = 320,
                    Text = _values.GetString(text.Key, text.Default),
                    Font = Theme.Body,
                    PlaceholderText = text.Placeholder ?? "",
                };
                tb.TextChanged += (_, _) => _onChanged(text.Key, tb.Text);
                return tb;
            }
            case NumberSetting num:
            {
                var nud = new NumericUpDown
                {
                    Minimum = (decimal)Math.Max(num.Min, (double)decimal.MinValue),
                    Maximum = (decimal)Math.Min(num.Max, (double)decimal.MaxValue),
                    DecimalPlaces = 2,
                    Increment = 1,
                    Width = 120,
                    Font = Theme.Body,
                };
                var current = _values.GetNumber(num.Key, num.Default);
                nud.Value = (decimal)Math.Clamp(current, (double)nud.Minimum, (double)nud.Maximum);
                nud.ValueChanged += (_, _) => _onChanged(num.Key, (double)nud.Value);
                return nud;
            }
            case DropdownSetting drop:
            {
                var combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 240,
                    Font = Theme.Body,
                };
                foreach (var opt in drop.Options) combo.Items.Add(opt);
                var current = _values.GetString(drop.Key, drop.Default);
                int idx = combo.Items.IndexOf(current);
                combo.SelectedIndex = idx >= 0 ? idx : Math.Max(0, combo.Items.IndexOf(drop.Default));
                combo.SelectedIndexChanged += (_, _) =>
                    _onChanged(drop.Key, combo.SelectedItem?.ToString() ?? "");
                return combo;
            }
            case FilePathSetting fp:
            {
                var row = new TableLayoutPanel
                {
                    ColumnCount = 2,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    BackColor = Color.Transparent,
                };
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                var tb = new TextBox
                {
                    Width = 320,
                    Text = _values.GetString(fp.Key, fp.Default),
                    Font = Theme.Body,
                };
                tb.TextChanged += (_, _) => _onChanged(fp.Key, tb.Text);
                var browse = new Button
                {
                    Text = "Browse…",
                    AutoSize = true,
                    Margin = new Padding(8, 0, 0, 0),
                };
                browse.Click += (_, _) =>
                {
                    using var dlg = new OpenFileDialog
                    {
                        Filter = fp.Filter ?? "All files (*.*)|*.*",
                        FileName = tb.Text,
                    };
                    if (dlg.ShowDialog(this) == DialogResult.OK) tb.Text = dlg.FileName;
                };
                row.Controls.Add(tb, 0, 0);
                row.Controls.Add(browse, 1, 0);
                return row;
            }
            case HotkeySetting hk:
            {
                var box = new HotkeyCaptureBox
                {
                    Width = 240,
                    Hotkey = _values.GetHotkey(hk.Key) ?? hk.Default,
                };
                box.HotkeyChanged += (_, _) =>
                    _onChanged(hk.Key, box.Hotkey?.ToString());
                return box;
            }
            default:
                return new Label
                {
                    Text = $"(unsupported setting type: {d.GetType().Name})",
                    ForeColor = Theme.SecondaryText,
                    Font = Theme.Body,
                    AutoSize = true,
                };
        }
    }
}
