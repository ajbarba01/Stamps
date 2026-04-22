using System.Windows.Forms;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Ui.Controls;

namespace Stamps.Ui.Pages;

/// <summary>
/// The landing page: a search box, a "show disabled" filter, and a vertical list of
/// <see cref="TweakCard"/>s. Clicking a card asks the host to navigate into the detail page;
/// the in-card toggle flips the tweak's enabled state without leaving the home view.
/// </summary>
/// <remarks>
/// Tweak enabled state is persisted under a synthetic per-tweak settings key (<c>__enabled</c>)
/// stored in the central app settings dictionary. Until Phase 3 introduces real tweaks, the
/// page renders an empty-state message instead of the list.
/// </remarks>
internal sealed class HomePage : UserControl, IPage
{
    private readonly TweakRegistry _registry;
    private readonly ISettingsStore _settings;
    private readonly Action<ITweak> _onOpenTweak;
    private readonly TextBox _search;
    private readonly CheckBox _showDisabled;
    private readonly FlowLayoutPanel _list;

    public string Title => "Tweaks";

    public HomePage(TweakRegistry registry, ISettingsStore settings, Action<ITweak> onOpenTweak)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(onOpenTweak);

        _registry = registry;
        _settings = settings;
        _onOpenTweak = onOpenTweak;

        Dock = DockStyle.Fill;
        BackColor = Theme.ContentBackground;
        Padding = new Padding(24);

        var filterBar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            Height = 36,
            BackColor = Color.Transparent,
        };
        filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        filterBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _search = new TextBox
        {
            PlaceholderText = "Search tweaks…",
            Font = Theme.Body,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 16, 4),
        };
        _search.TextChanged += (_, _) => Refresh_();

        _showDisabled = new CheckBox
        {
            Text = "Show disabled",
            Font = Theme.Body,
            AutoSize = true,
            Checked = true,
            Margin = new Padding(0, 8, 0, 0),
        };
        _showDisabled.CheckedChanged += (_, _) => Refresh_();

        filterBar.Controls.Add(_search,        column: 0, row: 0);
        filterBar.Controls.Add(_showDisabled,  column: 1, row: 0);

        _list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 16, 0, 0),
        };

        Controls.Add(_list);
        Controls.Add(filterBar);

        Refresh_();
    }

    private void Refresh_()
    {
        _list.SuspendLayout();
        foreach (Control c in _list.Controls) c.Dispose();
        _list.Controls.Clear();

        var query = _search.Text.Trim();
        var includeDisabled = _showDisabled.Checked;

        var matches = _registry.Tweaks
            .Where(t => string.IsNullOrEmpty(query)
                || t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Where(t => includeDisabled || IsTweakEnabled(t.Id))
            .ToList();

        if (matches.Count == 0)
        {
            _list.Controls.Add(new Label
            {
                Text = _registry.Tweaks.Count == 0
                    ? "No tweaks installed yet.\r\n\r\nThe first tweak (Snip) lands in Phase 3."
                    : "No tweaks match your search.",
                Font = Theme.Body,
                ForeColor = Theme.SecondaryText,
                AutoSize = true,
                Margin = new Padding(8, 16, 0, 0),
            });
        }
        else
        {
            foreach (var t in matches)
            {
                var card = new TweakCard(t, IsTweakEnabled(t.Id))
                {
                    Width = _list.ClientSize.Width - 4,
                };
                card.OpenRequested += (_, _) => _onOpenTweak(t);
                card.EnableToggled += (_, en) => SetTweakEnabled(t.Id, en);
                _list.Controls.Add(card);
            }
        }

        _list.ResumeLayout(true);
    }

    private bool IsTweakEnabled(string tweakId) =>
        !_settings.App.DisabledTweakIds.Contains(tweakId);

    private void SetTweakEnabled(string tweakId, bool enabled)
    {
        var disabled = _settings.App.DisabledTweakIds;
        bool changed = enabled ? disabled.Remove(tweakId) : disabled.Add(tweakId);
        if (changed) _settings.SaveApp();
    }
}
