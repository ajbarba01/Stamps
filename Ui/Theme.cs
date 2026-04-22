namespace Stamps.Ui;

/// <summary>
/// Centralised palette and font choices for the Stamps UI. Kept deliberately small — the
/// design language is "Windows 11 Settings" minimal, so most controls inherit defaults and
/// only the surfaces below get explicit styling.
/// </summary>
internal static class Theme
{
    public static readonly Color SidebarBackground = Color.FromArgb(249, 249, 249);
    public static readonly Color ContentBackground = Color.FromArgb(243, 243, 243);
    public static readonly Color CardBackground = Color.White;
    public static readonly Color CardBorder = Color.FromArgb(229, 229, 229);
    public static readonly Color Accent = Color.FromArgb(0, 103, 192);
    public static readonly Color AccentSubtle = Color.FromArgb(232, 240, 252);
    public static readonly Color HoverSubtle = Color.FromArgb(238, 238, 238);
    public static readonly Color SecondaryText = Color.FromArgb(96, 96, 96);
    public static readonly Color CodeBackground = Color.FromArgb(245, 245, 245);

    public static readonly Font H1 = new("Segoe UI", 22f, FontStyle.Bold);
    public static readonly Font H2 = new("Segoe UI", 16f, FontStyle.Bold);
    public static readonly Font H3 = new("Segoe UI", 13f, FontStyle.Bold);
    public static readonly Font Body = new("Segoe UI", 10f, FontStyle.Regular);
    public static readonly Font BodyBold = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font Code = new("Consolas", 10f, FontStyle.Regular);
    public static readonly Font NavItem = new("Segoe UI", 10.5f, FontStyle.Regular);
    public static readonly Font CardTitle = new("Segoe UI", 11.5f, FontStyle.Bold);
}
