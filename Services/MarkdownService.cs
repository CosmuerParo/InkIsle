using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace InkIsle.Services;

public sealed record HeadingLink(int Level, string Id, string Text);

public sealed class MarkdownService
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseAutoIdentifiers()
        .Build();

    public string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "";
        }

        return Markdown.ToHtml(markdown, Pipeline);
    }

    public string ToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return "";
        }

        return Markdown.ToPlainText(markdown, Pipeline).Trim();
    }

    public string BuildExcerpt(string? markdown, int maxLength = 160)
    {
        var text = Regex.Replace(ToPlainText(markdown), @"\s+", " ").Trim();
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength].TrimEnd() + "…";
    }

    public IReadOnlyList<HeadingLink> ExtractHeadings(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var result = new List<HeadingLink>();
        foreach (Match match in Regex.Matches(html, @"<h([23])\s+id=""([^""]+)""[^>]*>(.*?)</h\1>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var text = Regex.Replace(match.Groups[3].Value, "<[^>]+>", "");
            text = WebUtility.HtmlDecode(text).Trim();
            if (text.Length > 0)
            {
                result.Add(new HeadingLink(int.Parse(match.Groups[1].Value), match.Groups[2].Value, text));
            }
        }

        return result;
    }

    public IReadOnlyList<HeadingLink> ExtractHeadingsFromMarkdown(string? markdown)
    {
        return ExtractHeadings(ToHtml(markdown));
    }
}
