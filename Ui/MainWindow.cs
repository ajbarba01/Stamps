using System.Windows.Forms;
using Stamps.App;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Ui.Pages;

namespace Stamps.Ui;

/// <summary>
/// The Phase 2 main window. A narrow left sidebar with three flat entries (Tweaks, Settings,
/// About) hosts the navigation; the right pane swaps between <see cref="IPage"/>
/// implementations. Closing the window hides to tray.
/// </summary>
/// <remarks>
/// <para>
/// The tweak detail view is opened by clicking a card on the home page rather than from the
/// sidebar — the sidebar list stays short regardless of how many tweaks are installed. A
/// "Back" link returns the user to the home page.
/// </para>
/// <para>
/// Pages other than <see cref="TweakDetailPage"/> are constructed once and reused. The detail
/// page is constructed on-demand (it is parameterised by the chosen tweak) and disposed when
/// navigated away from to release the embedded <see cref="Controls.MarkdownView"/> children.
/// </para>
/// </remarks>
internal sealed class MainWindow : Form, IMainWindow
{
    private readonly Panel _content;
    private readonly Panel _header;
    private readonly Label _headerTitle;
    private readonly SidebarButton _navTweaks;
    private readonly SidebarButton _navSettings;
    private readonly SidebarButton _navAbout;

    private readonly HomePage _home;
    private readonly AppSettingsPage _appSettings;
    private readonly AboutPage _about;
    private TweakDetailPage? _activeDetail;
    private IPage? _currentPage;

    public MainWindow(
        TweakRegistry registry,
        ISettingsStore settings,
        IStartupManager startup,
        string logDirectory)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(startup);

        Text = "Stamps";
        Size = new Size(960, 640);
        MinimumSize = new Size(720, 480);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Theme.ContentBackground;
        Font = Theme.Body;
        ShowInTaskbar = true;

        _home = new HomePage(registry, settings, OpenTweakDetail);
        _appSettings = new AppSettingsPage(settings, startup);
        _about = new AboutPage(logDirectory);

        var sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 200,
            BackColor = Theme.SidebarBackground,
            Padding = new Padding(8, 16, 8, 16),
        };

        _navTweaks   = new SidebarButton("Tweaks");
        _navSettings = new SidebarButton("Settings");
        _navAbout    = new SidebarButton("About");

        _navTweaks.Click   += (_, _) => Navigate(_home);
        _navSettings.Click += (_, _) => Navigate(_appSettings);
        _navAbout.Click    += (_, _) => Navigate(_about);

        // FlowLayout adds children top-down with the most recently added at the bottom by
        // default; explicitly add in display order.
        var navStack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = Color.Transparent,
        };
        navStack.Controls.Add(_navTweaks);
        navStack.Controls.Add(_navSettings);
        navStack.Controls.Add(_navAbout);
        sidebar.Controls.Add(navStack);

        _headerTitle = new Label
        {
            Text = "Tweaks",
            Font = Theme.H1,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };
        _header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Theme.ContentBackground,
            Padding = new Padding(24, 0, 24, 0),
        };
        _header.Controls.Add(_headerTitle);

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.ContentBackground,
        };

        Controls.Add(_content);
        Controls.Add(_header);
        Controls.Add(sidebar);

        Navigate(_home);
    }

    public void ShowOrFocus()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(ShowOrFocus)); return; }

        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    bool IMainWindow.IsVisible => Visible;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    private void OpenTweakDetail(ITweak tweak)
    {
        _activeDetail?.Dispose();
        _activeDetail = new TweakDetailPage(tweak, onBack: () => Navigate(_home));
        Navigate(_activeDetail);
    }

    private void Navigate(IPage page)
    {
        if (ReferenceEquals(_currentPage, page)) return;

        _content.SuspendLayout();
        _content.Controls.Clear();
        if (page is Control control)
        {
            control.Dock = DockStyle.Fill;
            _content.Controls.Add(control);
        }
        _content.ResumeLayout(true);

        _currentPage = page;
        _headerTitle.Text = page.Title;
        UpdateNavSelection(page);

        // Tear down the previous detail instance once it is no longer the current page —
        // its README MarkdownView and the per-action sub-controls hold image and font
        // references we don't want to leak across navigations.
        if (page is not TweakDetailPage && _activeDetail is { } stale)
        {
            _activeDetail = null;
            stale.Dispose();
        }
    }

    private void UpdateNavSelection(IPage current)
    {
        _navTweaks.IsSelected   = current == _home || current is TweakDetailPage;
        _navSettings.IsSelected = current == _appSettings;
        _navAbout.IsSelected    = current == _about;
    }

    /// <summary>A tall, full-width sidebar entry with a hover and selected state.</summary>
    private sealed class SidebarButton : Label
    {
        private bool _hovered;
        private bool _selected;

        public SidebarButton(string text)
        {
            Text = text;
            Font = Theme.NavItem;
            AutoSize = false;
            Size = new Size(184, 36);
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(16, 0, 0, 0);
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 2, 0, 2);
        }

        public bool IsSelected
        {
            get => _selected;
            set { if (_selected != value) { _selected = value; UpdateColors(); } }
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hovered = true;  UpdateColors(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hovered = false; UpdateColors(); }

        private void UpdateColors()
        {
            BackColor = _selected ? Theme.AccentSubtle
                       : _hovered ? Theme.HoverSubtle
                       : Color.Transparent;
            ForeColor = _selected ? Theme.Accent : Color.Black;
        }
    }
}
