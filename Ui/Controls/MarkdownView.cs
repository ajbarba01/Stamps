using System.Text;
using System.Windows.Forms;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Stamps.Ui.Controls;

/// <summary>
/// Renders a small subset of Markdown into native WinForms controls — chosen over an embedded
/// browser so tweak READMEs look like part of the app and require no runtime dependencies.
/// </summary>
/// <remarks>
/// <para>Supported: ATX headings (H1–H3), paragraphs, ordered/unordered lists (with
/// nesting), fenced and indented code blocks, thematic breaks, and images via relative
/// paths. Inline emphasis and links are flattened into plain text — links are appended
/// as <c>text (url)</c> so the URL remains visible — which is sufficient for the short
/// structured docs we ship and avoids RichTextBox layout fragility. Tables and raw HTML
/// are intentionally not handled.</para>
/// <para>Layout is performed manually rather than through a <see cref="TableLayoutPanel"/>
/// because we need width-aware word wrap (each block's preferred height depends on the
/// available width) and have to recompute on every container resize.</para>
/// </remarks>
internal sealed class MarkdownView : Panel
{
    private const int OuterPad = 24;
    private const int BlockGap = 8;

    private readonly List<Control> _blocks = new();
    private string _baseDirectory = "";

    public MarkdownView()
    {
        AutoScroll = true;
        BackColor = Theme.ContentBackground;
        DoubleBuffered = true;
        Padding = Padding.Empty;
    }

    /// <summary>Loads and renders a Markdown file. Relative image paths resolve against
    /// the file's directory. Missing files render a friendly placeholder.</summary>
    public void LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _baseDirectory = Path.GetDirectoryName(path) ?? "";
        var text = File.Exists(path)
            ? File.ReadAllText(path)
            : $"# README missing\n\nExpected at: `{path}`";
        LoadMarkdown(text);
    }

    /// <summary>Renders a Markdown string. <see cref="LoadFromFile"/> is preferred when
    /// images may appear; this overload resolves images against the current base directory.</summary>
    public void LoadMarkdown(string markdown)
    {
        SuspendLayout();
        foreach (var c in _blocks) c.Dispose();
        _blocks.Clear();
        Controls.Clear();

        var doc = Markdown.Parse(markdown ?? "");
        foreach (var block in doc) RenderBlock(block, indent: 0);

        foreach (var c in _blocks) Controls.Add(c);
        ResumeLayout(performLayout: true);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        int width = ClientSize.Width - 2 * OuterPad;
        if (width < 50) width = 50;

        int y = OuterPad;
        foreach (var control in _blocks)
        {
            int blockWidth = width - control.Margin.Horizontal;
            int height = MeasureBlockHeight(control, blockWidth);

            control.Bounds = new Rectangle(
                OuterPad + control.Margin.Left,
                y + control.Margin.Top,
                blockWidth,
                height);

            y += control.Margin.Vertical + height + BlockGap;
        }

        AutoScrollMinSize = new Size(0, y + OuterPad);
        base.OnLayout(levent);
    }

    private static int MeasureBlockHeight(Control control, int width)
    {
        if (control is Label label)
        {
            label.MaximumSize = new Size(width, 0);
            return label.GetPreferredSize(new Size(width, 0)).Height;
        }
        if (control is PictureBox pic && pic.Image is { } img)
        {
            double scale = Math.Min(1.0, (double)width / img.Width);
            return Math.Max(1, (int)(img.Height * scale));
        }
        if (control is TextBox tb)
        {
            int lines = Math.Max(1, tb.Lines.Length);
            return tb.Font.Height * lines + 16;
        }
        return control.GetPreferredSize(new Size(width, 0)).Height;
    }

    private void RenderBlock(Block block, int indent)
    {
        switch (block)
        {
            case HeadingBlock h:
                Add(new Label
                {
                    Text = FlattenInlines(h.Inline),
                    Font = h.Level switch { 1 => Theme.H1, 2 => Theme.H2, _ => Theme.H3 },
                    AutoSize = false,
                    BackColor = Color.Transparent,
                    Margin = new Padding(indent, h.Level == 1 ? 0 : 12, 0, 4),
                });
                break;

            case ParagraphBlock p:
                Add(new Label
                {
                    Text = FlattenInlines(p.Inline),
                    Font = Theme.Body,
                    AutoSize = false,
                    BackColor = Color.Transparent,
                    Margin = new Padding(indent, 0, 0, 0),
                });
                AddImagesFromInlines(p.Inline, indent);
                break;

            case ListBlock list:
                int idx = 1;
                foreach (var item in list)
                {
                    if (item is ListItemBlock li)
                    {
                        var prefix = list.IsOrdered ? $"{idx}. " : "• ";
                        RenderListItem(li, indent, prefix);
                        idx++;
                    }
                }
                break;

            case FencedCodeBlock fenced:
                Add(BuildCodeBlock(fenced.Lines.ToString(), indent));
                break;

            case CodeBlock code:
                Add(BuildCodeBlock(code.Lines.ToString(), indent));
                break;

            case ThematicBreakBlock:
                Add(new Panel
                {
                    Height = 1,
                    BackColor = Theme.CardBorder,
                    Margin = new Padding(indent, 8, 0, 8),
                });
                break;

            case QuoteBlock quote:
                foreach (var inner in quote)
                    RenderBlock(inner, indent + 16);
                break;
        }
    }

    private void RenderListItem(ListItemBlock item, int indent, string prefix)
    {
        bool firstParaHandled = false;
        foreach (var child in item)
        {
            if (!firstParaHandled && child is ParagraphBlock para)
            {
                Add(new Label
                {
                    Text = prefix + FlattenInlines(para.Inline),
                    Font = Theme.Body,
                    AutoSize = false,
                    BackColor = Color.Transparent,
                    Margin = new Padding(indent + 8, 0, 0, 0),
                });
                AddImagesFromInlines(para.Inline, indent + 24);
                firstParaHandled = true;
            }
            else
            {
                RenderBlock(child, indent + 24);
            }
        }
    }

    private TextBox BuildCodeBlock(string text, int indent)
    {
        var trimmed = text.TrimEnd('\n', '\r');
        return new TextBox
        {
            Text = trimmed,
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.CodeBackground,
            Font = Theme.Code,
            ScrollBars = ScrollBars.None,
            WordWrap = false,
            Margin = new Padding(indent, 4, 0, 4),
        };
    }

    private void AddImagesFromInlines(ContainerInline? container, int indent)
    {
        if (container is null) return;
        foreach (var inline in container)
        {
            if (inline is LinkInline { IsImage: true } image && !string.IsNullOrEmpty(image.Url))
                Add(BuildImage(image.Url!, indent));
        }
    }

    private PictureBox BuildImage(string relativeUrl, int indent)
    {
        var box = new PictureBox
        {
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
            Margin = new Padding(indent, 6, 0, 6),
        };
        try
        {
            var resolved = Path.IsPathRooted(relativeUrl)
                ? relativeUrl
                : Path.Combine(_baseDirectory, relativeUrl);
            if (File.Exists(resolved))
            {
                using var stream = File.OpenRead(resolved);
                box.Image = Image.FromStream(stream);
            }
        }
        catch { /* missing or invalid image — leave the box empty rather than crash */ }
        return box;
    }

    private void Add(Control control) => _blocks.Add(control);

    private static string FlattenInlines(ContainerInline? container)
    {
        if (container is null) return "";
        var sb = new StringBuilder();
        FlattenInto(container, sb);
        return sb.ToString();
    }

    private static void FlattenInto(ContainerInline container, StringBuilder sb)
    {
        foreach (var inline in container)
        {
            switch (inline)
            {
                case LiteralInline lit:
                    sb.Append(lit.Content.ToString());
                    break;
                case CodeInline code:
                    sb.Append(code.Content);
                    break;
                case EmphasisInline em:
                    FlattenInto(em, sb);
                    break;
                case LinkInline { IsImage: true }:
                    // Images are rendered separately as PictureBox; skip the alt text here
                    // to avoid a stray "alt" string appearing in the paragraph.
                    break;
                case LinkInline link:
                    var linkText = new StringBuilder();
                    FlattenInto(link, linkText);
                    sb.Append(linkText);
                    if (!string.IsNullOrEmpty(link.Url) && link.Url != linkText.ToString())
                        sb.Append(" (").Append(link.Url).Append(')');
                    break;
                case LineBreakInline:
                    sb.Append('\n');
                    break;
            }
        }
    }

}
