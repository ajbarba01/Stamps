using System.Windows.Forms;

namespace Stamps.Ui.Pages;

/// <summary>
/// Marker interface for the swappable content panes hosted in <see cref="MainWindow"/>.
/// Pages are full <see cref="UserControl"/>s so they own their own layout; the host only
/// reads the title and toggles visibility.
/// </summary>
internal interface IPage
{
    /// <summary>Title shown in the page header above the content area.</summary>
    string Title { get; }
}
