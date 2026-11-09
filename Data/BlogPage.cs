using System.ComponentModel.DataAnnotations;

namespace InkIsle.Data;

public class BlogPage
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = "";

    [MaxLength(160)]
    public string Slug { get; set; } = "";

    [MaxLength(500)]
    public string Summary { get; set; } = "";

    public string ContentMarkdown { get; set; } = "";

    public bool IsPublished { get; set; } = true;

    public bool ShowInNavigation { get; set; }

    public int SortOrder { get; set; }

    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    [MaxLength(300)]
    public string? SeoDescription { get; set; }

    public string? CustomCss { get; set; }

    public string? CustomJs { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
