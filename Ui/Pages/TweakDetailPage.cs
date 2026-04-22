using System.Windows.Forms;
using Stamps.Core;
using Stamps.Ui.Controls;

namespace Stamps.Ui.Pages;

/// <summary>
/// The per-tweak detail view, opened by clicking a card on <see cref="HomePage"/>. Layout:
/// a back button + tweak header at the top, then a tab control with Overview (README),
/// Actions (per-action enable + hotkey + settings), and Settings (tweak-level settings).
/// </summary>
/// <remarks>
/// Per-action and per-tweak setting values flow through the standard
/// <see cref="SettingDescriptor"/> + <see cref="SettingsValues"/> pair; the page is therefore
/// agnostic to the concrete tweak. Persistence is delegated to the tweak via the
/// <see cref="ITweak.PersistSettings"/> / <see cref="IAction.PersistSettings"/> hooks.
/// </remarks>
internal sealed class TweakDetailPage : UserControl, IPage
{
    private readonly ITweak _tweak;

    public string Title => _tweak.Title;

    public TweakDetailPage(ITweak tweak, Action onBack)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        ArgumentNullException.ThrowIfNull(onBack);
        _tweak = tweak;

        Dock = DockStyle.Fill;
        BackColor = Theme.ContentBackground;
        Padding = new Padding(24);

        var back = new LinkLabel
        {
            Text = "← Back to Tweaks",
            AutoSize = true,
            Font = Theme.Body,
            LinkColor = Theme.Accent,
            Margin = new Padding(0, 0, 0, 8),
        };
        back.LinkClicked += (_, _) => onBack();

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Theme.Body,
        };

        tabs.TabPages.Add(BuildOverviewTab());
        tabs.TabPages.Add(BuildActionsTab());
        tabs.TabPages.Add(BuildSettingsTab());

        var headerStack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
        };
        headerStack.Controls.Add(back);
        headerStack.Controls.Add(new Label
        {
            Text = _tweak.Description,
            Font = Theme.Body,
            ForeColor = Theme.SecondaryText,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12),
        });

        Controls.Add(tabs);
        Controls.Add(headerStack);
    }

    private TabPage BuildOverviewTab()
    {
        var page = new TabPage("Overview") { BackColor = Theme.ContentBackground, Padding = new Padding(0) };
        var view = new MarkdownView { Dock = DockStyle.Fill };

        var readmePath = Path.Combine(
            AppContext.BaseDirectory, "Tweaks", _tweak.Id, "README.md");
        view.LoadFromFile(readmePath);

        page.Controls.Add(view);
        return page;
    }

    private TabPage BuildActionsTab()
    {
        var page = new TabPage("Actions") { BackColor = Theme.ContentBackground, Padding = new Padding(16) };
        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            BackColor = Color.Transparent,
        };

        if (_tweak.BuiltInActions.Count == 0 && _tweak.UserActionFactory is null)
        {
            stack.Controls.Add(new Label
            {
                Text = "This tweak exposes no actions.",
                ForeColor = Theme.SecondaryText,
                Font = Theme.Body,
                AutoSize = true,
            });
        }

        foreach (var action in _tweak.BuiltInActions)
            stack.Controls.Add(BuildActionRow(action));

        if (_tweak.UserActionFactory is { } factory)
        {
            var addBtn = new Button
            {
                Text = factory.CreateButtonLabel,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0),
            };
            // Phase 2 ships without the user-action creation dialog; the button is wired
            // in Phase 3 alongside the first tweak that exposes a factory.
            addBtn.Enabled = false;
            stack.Controls.Add(addBtn);
        }

        page.Controls.Add(stack);
        return page;
    }

    private Control BuildActionRow(IAction action)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.CardBackground,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 8),
            Width = 720,
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var headerRow = new TableLayoutPanel
        {
            ColumnCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
        };
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        headerRow.Controls.Add(new Label
        {
            Text = action.Title,
            Font = Theme.BodyBold,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        }, 0, 0);

        var hotkeyBox = new HotkeyCaptureBox
        {
            Width = 220,
            Hotkey = action.Hotkey,
            Margin = new Padding(8, 0, 8, 0),
        };
        hotkeyBox.HotkeyChanged += (_, _) =>
        {
            action.Hotkey = hotkeyBox.Hotkey;
            action.PersistSettings();
        };
        headerRow.Controls.Add(hotkeyBox, 1, 0);

        var enabledBox = new CheckBox
        {
            Text = "Enabled",
            Checked = action.Enabled,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0),
            Font = Theme.Body,
        };
        enabledBox.CheckedChanged += (_, _) =>
        {
            action.Enabled = enabledBox.Checked;
            action.PersistSettings();
        };
        headerRow.Controls.Add(enabledBox, 2, 0);

        row.Controls.Add(headerRow);

        if (action.Settings.Count > 0)
        {
            var sub = new SettingsPanel(action.Settings, action.Values, (key, value) =>
            {
                action.Values.Set(key, value);
                action.PersistSettings();
            })
            {
                Margin = new Padding(0, 8, 0, 0),
            };
            row.Controls.Add(sub);
        }

        return row;
    }

    private TabPage BuildSettingsTab()
    {
        var page = new TabPage("Settings") { BackColor = Theme.ContentBackground, Padding = new Padding(16) };

        var custom = _tweak.CreateCustomSettingsControl();
        if (custom != null)
        {
            custom.Dock = DockStyle.Fill;
            page.Controls.Add(custom);
            return page;
        }

        var panel = new SettingsPanel(_tweak.Settings, _tweak.Values, (key, value) =>
        {
            _tweak.Values.Set(key, value);
            _tweak.PersistSettings();
        })
        {
            Dock = DockStyle.Top,
        };
        page.Controls.Add(panel);
        return page;
    }
}
