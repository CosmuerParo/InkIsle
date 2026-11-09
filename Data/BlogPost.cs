using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InkIsle.Data;

public enum PostStatus
{
    Draft = 0,
    Published = 1
}

public class BlogPost
{
    public int Id { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = "";

    [MaxLength(160)]
    public string Slug { get; set; } = "";

    [MaxLength(500)]
    public string Summary { get; set; } = "";

    public string ContentMarkdown { get; set; } = "";

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    [MaxLength(300)]
    public string Tags { get; set; } = "";

    public bool IsFeatured { get; set; }

    public PostStatus Status { get; set; } = PostStatus.Draft;

    [MaxLength(200)]
    public string? SeoTitle { get; set; }

    [MaxLength(300)]
    public string? SeoDescription { get; set; }

    public string? CustomCss { get; set; }

    public string? CustomJs { get; set; }

    [MaxLength(80)]
    public string AuthorName { get; set; } = "墨屿";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedAtUtc { get; set; }

    public int ViewCount { get; set; }

    [NotMapped]
    public IReadOnlyList<string> TagList => TagHelper.Parse(Tags);

    [NotMapped]
    public bool IsPublished => Status == PostStatus.Published;

    [NotMapped]
    public string DisplayDate => ChinaTime.FormatDate(PublishedAtUtc ?? CreatedAtUtc);
}
