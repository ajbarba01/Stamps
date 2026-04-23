using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Appearance;

namespace Stamps.Ui.Theme;

public static class ThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    // Undocumented uxtheme ordinals — required for dark system context menus (Restore/Move/etc.)
    // AllowDark=1, ForceDark=2, ForceLight=3
    [DllImport("uxtheme.dll", EntryPoint = "#135")]
    private static extern int SetPreferredAppMode(int mode);

    [DllImport("uxtheme.dll", EntryPoint = "#132")]
    private static extern bool AllowDarkModeForWindow(nint hwnd, bool allow);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    private static extern void FlushMenuThemes();

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attr, ref int attrValue, int attrSize);

    public static AppTheme Current { get; private set; } = AppTheme.System;

    public static event EventHandler? ThemeChanged;

    public static void Apply(AppTheme theme)
    {
        Current = theme;
        var resolved = theme == AppTheme.System ? DetectSystemTheme() : theme;

        // ForceDark=2, ForceLight=3 — makes system menus follow our chosen theme, not the OS setting
        SetPreferredAppMode(resolved == AppTheme.Dark ? 2 : 3);
        FlushMenuThemes();

        ApplicationThemeManager.Apply(
            resolved == AppTheme.Dark ? ApplicationTheme.Dark : ApplicationTheme.Light);

        SwapPalette(resolved);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void ApplyToHwnd(nint hwnd)
    {
        var resolved = Current == AppTheme.System ? DetectSystemTheme() : Current;
        bool dark = resolved == AppTheme.Dark;
        int darkInt = dark ? 1 : 0;
        AllowDarkModeForWindow(hwnd, dark);
        DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref darkInt, sizeof(int));
        FlushMenuThemes();
    }

    public static AppTheme DetectSystemTheme()
    {
        var value = Registry.CurrentUser
            .OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize")
            ?.GetValue("AppsUseLightTheme");
        return value is int v && v == 0 ? AppTheme.Dark : AppTheme.Light;
    }

    private static void SwapPalette(AppTheme theme)
    {
        var dicts = Application.Current.Resources.MergedDictionaries;
        var old = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("/Palettes/", StringComparison.Ordinal) == true);
        if (old != null) dicts.Remove(old);

        var name = theme == AppTheme.Dark ? "Dark" : "Light";
        dicts.Add(new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Stamps;component/Ui/Theme/Palettes/{name}.xaml")
        });
    }
}
