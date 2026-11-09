using System.ComponentModel.DataAnnotations;

namespace InkIsle.Data;

public class SiteSettings
{
    public int Id { get; set; } = 1;

    [MaxLength(80)]
    public string SiteTitle { get; set; } = "墨屿";

    [MaxLength(120)]
    public string Tagline { get; set; } = "在文字的岛屿上，慢一点。";

    [MaxLength(300)]
    public string Description { get; set; } = "一个关于写作、技术与生活的安静角落。";

    [MaxLength(80)]
    public string HeroKicker { get; set; } = "A QUIET PLACE FOR IDEAS";

    [MaxLength(160)]
    public string HeroTitle { get; set; } = "在文字的岛屿上，慢一点。";

    [MaxLength(500)]
    public string HeroSubtitle { get; set; } = "记录技术、阅读与日常的微光。这里没有信息瀑布，只有值得反复阅读的句子。";

    [MaxLength(80)]
    public string AuthorName { get; set; } = "墨屿主人";

    [MaxLength(500)]
    public string AuthorBio { get; set; } = "写代码，也写字。相信长期主义和缓慢的积累。";

    [MaxLength(200)]
    public string FooterText { get; set; } = "愿每一次抵达，都有文字留下回声。";

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? GithubUrl { get; set; }

    [MaxLength(16)]
    public string AccentColor { get; set; } = "#c65a3f";

    public string? SiteWideCss { get; set; }

    public string? SiteWideJs { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
