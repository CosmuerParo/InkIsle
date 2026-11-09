using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace InkIsle.Data;

public static partial class TagHelper
{
    private static readonly char[] Separators = [',', '，', ';', '；', '、', '#'];

    public static IReadOnlyList<string> Parse(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        return tags.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
    }

    public static string Normalize(string? tags) => string.Join(", ", Parse(tags));
}

public static partial class SlugHelper
{
    public static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "account", "api", "blog", "tag", "archive", "search", "feed.xml",
        "sitemap.xml", "robots.txt", "uploads", "css", "js", "images", "favicon.ico",
        "favicon.svg", "error", "login", "logout", "not-found", "_framework", "health"
    };

    public static string Create(string? source, string fallbackPrefix = "post")
    {
        var normalized = (source ?? "").Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var lastWasDash = false;

        foreach (var raw in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(raw);
            if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark)
            {
                continue;
            }

            var c = char.ToLowerInvariant(raw);
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(c);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        if (slug.Length > 120)
        {
            slug = slug[..120].TrimEnd('-');
        }

        if (slug.Length < 1)
        {
            slug = $"{fallbackPrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        }

        if (Reserved.Contains(slug))
        {
            slug = $"{slug}-{DateTime.UtcNow:MMddHHmm}";
        }

        return slug;
    }

    public static string CreateUnique(string source, IEnumerable<string> existing, string fallbackPrefix = "post")
    {
        var baseSlug = Create(source, fallbackPrefix);
        var slug = baseSlug;
        var used = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var index = 2;

        while (used.Contains(slug) || Reserved.Contains(slug))
        {
            slug = $"{baseSlug}-{index++}";
        }

        return slug;
    }

    public static bool IsSane(string? slug) => !string.IsNullOrWhiteSpace(slug) &&
        SlugPattern().IsMatch(slug) && !Reserved.Contains(slug);

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();
}

public static class ChinaTime
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Asia/Shanghai", "China Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    public static DateTime FromUtc(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
    }

    public static string FormatDate(DateTime value) => FromUtc(value).ToString("yyyy年M月d日");

    public static string FormatDateTime(DateTime value) => FromUtc(value).ToString("yyyy年M月d日 HH:mm");

    public static string FormatIso(DateTime value) => FromUtc(value).ToString("yyyy-MM-ddTHH:mm:sszzz");

    public static string MonthLabel(DateTime value) => FromUtc(value).ToString("yyyy年M月");
}

public static class ReadingTime
{
    public static int EstimateMinutes(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return 1;
        }

        var chineseChars = markdown.Count(c => c is >= '\u4e00' and <= '\u9fff');
        var words = Regex.Matches(markdown, @"[A-Za-z0-9]+").Count;
        var minutes = (int)Math.Ceiling((chineseChars / 350d) + (words / 220d));
        return Math.Max(1, minutes);
    }
}
