using System.Windows;
using System.Windows.Controls;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Ui.Controls;

namespace Stamps.Ui.Pages;

public partial class HomePage : UserControl, IPage
{
    private readonly TweakRegistry _registry;
    private readonly ISettingsStore _settings;
    private readonly Action<ITweak> _onOpenTweak;

    public string Title => "Tweaks";

    public HomePage(TweakRegistry registry, ISettingsStore settings, Action<ITweak> onOpenTweak)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(onOpenTweak);

        _registry = registry;
        _settings = settings;
        _onOpenTweak = onOpenTweak;

        InitializeComponent();
        Refresh();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => Refresh();
    private void OnFilterChanged(object sender, RoutedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (TweakList is null) return;
        TweakList.Children.Clear();

        var query = SearchBox.Text.Trim();
        var includeDisabled = ShowDisabledBox.IsChecked == true;

        var matches = _registry.Tweaks
            .Where(t => string.IsNullOrEmpty(query)
                || t.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Where(t => includeDisabled || IsTweakEnabled(t.Id))
            .ToList();

        if (matches.Count == 0)
        {
            TweakList.Children.Add(new TextBlock
            {
                Text = _registry.Tweaks.Count == 0
                    ? "No tweaks installed yet.\n\nThe first tweak (Snip) lands in Phase 3."
                    : "No tweaks match your search.",
                FontFamily = (System.Windows.Media.FontFamily)FindResource("Stamps.FontFamily"),
                Foreground = (System.Windows.Media.Brush)FindResource("Stamps.Text.Secondary"),
                Margin = new Thickness(4, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            });
            return;
        }

        foreach (var t in matches)
        {
            var card = new TweakCard(t, IsTweakEnabled(t.Id));
            card.OpenRequested += (_, _) => _onOpenTweak(t);
            card.EnableToggled += (_, en) => SetTweakEnabled(t.Id, en);
            TweakList.Children.Add(card);
        }
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
