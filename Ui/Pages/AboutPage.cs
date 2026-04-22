using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Stamps.Ui.Pages;

/// <summary>
/// Static "About" page: app name, build version, and a button to reveal the user log
/// directory in Explorer (the most useful diagnostic action a user can take unaided).
/// </summary>
internal sealed class AboutPage : UserControl, IPage
{
    public string Title => "About";

    public AboutPage(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);

        Dock = DockStyle.Fill;
        BackColor = Theme.ContentBackground;
        Padding = new Padding(24);

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            BackColor = Color.Transparent,
        };

        stack.Controls.Add(new Label
        {
            Text = "Stamps",
            Font = Theme.H1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
        });
        stack.Controls.Add(new Label
        {
            Text = $"Version {version}",
            Font = Theme.Body,
            ForeColor = Theme.SecondaryText,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
        });
        stack.Controls.Add(new Label
        {
            Text = "A tray-resident control panel for a growing library of small Windows tweaks.",
            Font = Theme.Body,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 24),
        });

        var openLogs = new Button
        {
            Text = "Open log folder",
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4),
        };
        openLogs.Click += (_, _) =>
        {
            try
            {
                Directory.CreateDirectory(logDirectory);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logDirectory}\"")
                {
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Surfacing the failure to the user via a message box would be acceptable
                // here, but a missing log folder is rare enough that the silent failure is
                // preferable to interrupting them with a dialog about diagnostics.
            }
        };
        stack.Controls.Add(openLogs);

        Controls.Add(stack);
    }
}
