using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdBlock = Markdig.Syntax.Block;

namespace Stamps.Ui.Controls;

public partial class MarkdownView : UserControl
{
    private string _baseDirectory = "";

    public MarkdownView() => InitializeComponent();

    public void LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _baseDirectory = Path.GetDirectoryName(path) ?? "";
        var text = File.Exists(path)
            ? File.ReadAllText(path)
            : $"# README missing\n\nExpected at: `{path}`";
        LoadMarkdown(text);
    }

    public void LoadMarkdown(string markdown)
    {
        Container.Children.Clear();
        var doc = Markdown.Parse(markdown ?? "");
        foreach (MdBlock block in doc)
            RenderBlock(block, indent: 0);
    }

    private void RenderBlock(MdBlock block, int indent)
    {
        switch (block)
        {
            case HeadingBlock h:
                Add(new TextBlock
                {
                    Text = FlattenInlines(h.Inline),
                    FontFamily = GetFont(),
                    FontSize = h.Level switch { 1 => 24, 2 => 18, _ => 14 },
                    FontWeight = FontWeights.SemiBold,
                    Foreground = GetBrush("Stamps.Text.Primary"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(indent, h.Level == 1 ? 0 : 12, 0, 4),
                });
                break;

            case ParagraphBlock p:
                Add(new TextBlock
                {
                    Text = FlattenInlines(p.Inline),
                    FontFamily = GetFont(),
                    FontSize = 13,
                    Foreground = GetBrush("Stamps.Text.Primary"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(indent, 0, 0, 4),
                });
                AddImagesFromInlines(p.Inline, indent);
                break;

            case ListBlock list:
                int idx = 1;
                foreach (var item in list)
                {
                    if (item is ListItemBlock li)
                    {
                        RenderListItem(li, indent, list.IsOrdered ? $"{idx}. " : "• ");
                        idx++;
                    }
                }
                break;

            case FencedCodeBlock fenced: Add(BuildCodeBlock(fenced.Lines.ToString(), indent)); break;
            case CodeBlock code:         Add(BuildCodeBlock(code.Lines.ToString(), indent)); break;

            case ThematicBreakBlock:
                Add(new Border
                {
                    Height = 1,
                    Background = GetBrush("Stamps.Border.Default"),
                    Margin = new Thickness(indent, 8, 0, 8),
                });
                break;

            case QuoteBlock quote:
                foreach (MdBlock inner in quote) RenderBlock(inner, indent + 16);
                break;
        }
    }

    private void RenderListItem(ListItemBlock item, int indent, string prefix)
    {
        bool first = true;
        foreach (var child in item)
        {
            if (first && child is ParagraphBlock para)
            {
                Add(new TextBlock
                {
                    Text = prefix + FlattenInlines(para.Inline),
                    FontFamily = GetFont(),
                    FontSize = 13,
                    Foreground = GetBrush("Stamps.Text.Primary"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(indent + 8, 0, 0, 2),
                });
                AddImagesFromInlines(para.Inline, indent + 24);
                first = false;
            }
            else { RenderBlock((MdBlock)child, indent + 24); }
        }
    }

    private UIElement BuildCodeBlock(string text, int indent)
    {
        return new Border
        {
            Background = GetBrush("Stamps.Background.Card"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(indent, 4, 0, 4),
            Child = new TextBlock
            {
                Text = text.TrimEnd('\n', '\r'),
                FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                FontSize = 12,
                Foreground = GetBrush("Stamps.Text.Primary"),
                TextWrapping = TextWrapping.NoWrap,
            },
        };
    }

    private void AddImagesFromInlines(ContainerInline? container, int indent)
    {
        if (container is null) return;
        foreach (var inline in container)
        {
            if (inline is LinkInline { IsImage: true } img && !string.IsNullOrEmpty(img.Url))
                Add(BuildImage(img.Url!, indent));
        }
    }

    private UIElement BuildImage(string relUrl, int indent)
    {
        var image = new Image
        {
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(indent, 6, 0, 6),
        };
        try
        {
            var resolved = Path.IsPathRooted(relUrl)
                ? relUrl : Path.Combine(_baseDirectory, relUrl);
            if (File.Exists(resolved))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(resolved, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                image.Source = bmp;
            }
        }
        catch { }
        return image;
    }

    private void Add(UIElement element) => Container.Children.Add(element);

    private FontFamily GetFont() =>
        TryFindResource("Stamps.FontFamily") as FontFamily
        ?? new FontFamily("Segoe UI Variable, Segoe UI");

    private Brush GetBrush(string key) =>
        TryFindResource(key) as Brush ?? Brushes.White;

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
                case LiteralInline lit: sb.Append(lit.Content.ToString()); break;
                case CodeInline code:   sb.Append(code.Content); break;
                case EmphasisInline em: FlattenInto(em, sb); break;
                case LinkInline { IsImage: true }: break;
                case LinkInline link:
                    var lt = new StringBuilder();
                    FlattenInto(link, lt);
                    sb.Append(lt);
                    if (!string.IsNullOrEmpty(link.Url) && link.Url != lt.ToString())
                        sb.Append(" (").Append(link.Url).Append(')');
                    break;
                case LineBreakInline: sb.Append('\n'); break;
            }
        }
    }
}
