using InkIsle.Data;
using Microsoft.EntityFrameworkCore;

namespace InkIsle.Services;

public sealed record PagedPosts(IReadOnlyList<BlogPost> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

public sealed record PostNeighbors(BlogPost? Newer, BlogPost? Older);

public sealed record TagCloudItem(string Name, int Count);

public sealed record ArchiveGroup(string Year, IReadOnlyList<BlogPost> Posts);

public sealed record AdminStats(int Published, int Drafts, int Pages, int Views, int Tags);

public sealed class BlogService(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    private IQueryable<BlogPost> PublishedQuery(ApplicationDbContext db) =>
        db.Posts.AsNoTracking()
            .Where(p => p.Status == PostStatus.Published && p.PublishedAtUtc != null);

    public async Task<PagedPosts> GetPublishedAsync(
        int page = 1,
        int pageSize = 9,
        string? query = null,
        string? tag = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 30);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var posts = PublishedQuery(db);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            posts = posts.Where(p => p.Title.Contains(term) || p.Summary.Contains(term) || p.ContentMarkdown.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            var cleanTag = tag.Trim();
            posts = posts.Where(p => EF.Functions.Like(p.Tags, $"%{cleanTag}%"));
        }

        var total = await posts.CountAsync(cancellationToken);
        var items = await posts
            .OrderByDescending(p => p.PublishedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedPosts(items, total, page, pageSize);
    }

    public async Task<BlogPost?> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await PublishedQuery(db).FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task IncrementViewAsync(int postId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await db.Posts.Where(p => p.Id == postId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ViewCount, p => p.ViewCount + 1), cancellationToken);
    }

    public async Task<List<BlogPost>> GetFeaturedAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await PublishedQuery(db)
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PublishedAtUtc)
            .Take(Math.Clamp(count, 1, 6))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BlogPost>> GetRelatedAsync(BlogPost post, int count = 3, CancellationToken cancellationToken = default)
    {
        var tags = post.TagList;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = PublishedQuery(db).Where(p => p.Id != post.Id);

        if (tags.Count > 0)
        {
            var first = tags[0];
            query = query.Where(p => p.Tags.Contains(first) ||
                (tags.Count > 1 && p.Tags.Contains(tags[1])) ||
                (tags.Count > 2 && p.Tags.Contains(tags[2])));
        }

        return await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.PublishedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<PostNeighbors> GetNeighborsAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var date = post.PublishedAtUtc ?? post.CreatedAtUtc;

        var newer = await PublishedQuery(db)
            .Where(p => p.Id != post.Id && p.PublishedAtUtc > date)
            .OrderBy(p => p.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var older = await PublishedQuery(db)
            .Where(p => p.Id != post.Id && p.PublishedAtUtc < date)
            .OrderByDescending(p => p.PublishedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new PostNeighbors(newer, older);
    }

    public async Task<List<TagCloudItem>> GetTagsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var raw = await PublishedQuery(db).Select(p => p.Tags).ToListAsync(cancellationToken);

        return raw
            .SelectMany(TagHelper.Parse)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TagCloudItem(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name)
            .Take(30)
            .ToList();
    }

    public async Task<List<ArchiveGroup>> GetArchiveAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var posts = await PublishedQuery(db)
            .OrderByDescending(p => p.PublishedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        return posts
            .GroupBy(p => ChinaTime.FromUtc(p.PublishedAtUtc!.Value).Year.ToString())
            .Select(g => new ArchiveGroup(g.Key, g.ToList()))
            .ToList();
    }

    public async Task<List<BlogPage>> GetNavPagesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pages.AsNoTracking()
            .Where(p => p.IsPublished && p.ShowInNavigation)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BlogPage>> GetPublishedPagesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pages.AsNoTracking()
            .Where(p => p.IsPublished)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlogPage?> GetPublishedPageAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished, cancellationToken);
    }

    public async Task<List<BlogPost>> GetFeedAsync(int count = 30, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await PublishedQuery(db)
            .OrderByDescending(p => p.PublishedAtUtc)
            .Take(Math.Clamp(count, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminStats> GetAdminStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var published = await db.Posts.CountAsync(p => p.Status == PostStatus.Published, cancellationToken);
        var drafts = await db.Posts.CountAsync(p => p.Status == PostStatus.Draft, cancellationToken);
        var pages = await db.Pages.CountAsync(cancellationToken);
        var views = await db.Posts.SumAsync(p => p.ViewCount, cancellationToken);
        var rawTags = await db.Posts.Where(p => p.Status == PostStatus.Published).Select(p => p.Tags).ToListAsync(cancellationToken);
        var tags = rawTags.SelectMany(TagHelper.Parse).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return new AdminStats(published, drafts, pages, views, tags);
    }

    public async Task<List<BlogPost>> GetRecentForAdminAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Posts.AsNoTracking()
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BlogPost>> GetAllPostsAsync(string? status = null, string? query = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var posts = db.Posts.AsNoTracking();

        if (string.Equals(status, "published", StringComparison.OrdinalIgnoreCase))
        {
            posts = posts.Where(p => p.Status == PostStatus.Published);
        }
        else if (string.Equals(status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            posts = posts.Where(p => p.Status == PostStatus.Draft);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            posts = posts.Where(p => p.Title.Contains(term) || p.Slug.Contains(term));
        }

        return await posts.OrderByDescending(p => p.UpdatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<List<BlogPost>> GetMonthlyPublishedAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.AddMonths(-months + 1);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await PublishedQuery(db)
            .Where(p => p.PublishedAtUtc >= from)
            .OrderBy(p => p.PublishedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlogPost?> GetPostByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> PostSlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Posts.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), cancellationToken);
    }

    public async Task<BlogPost> SavePostAsync(BlogPost post, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        post.UpdatedAtUtc = DateTime.UtcNow;
        post.Tags = TagHelper.Normalize(post.Tags);

        if (post.Id == 0)
        {
            post.CreatedAtUtc = DateTime.UtcNow;
            db.Posts.Add(post);
        }
        else
        {
            db.Posts.Update(post);
        }

        await db.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task DeletePostAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var post = await db.Posts.FindAsync([id], cancellationToken);
        if (post is not null)
        {
            db.Posts.Remove(post);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<BlogPage>> GetAllPagesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pages.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<BlogPage?> GetPageByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<bool> PageSlugExistsAsync(string slug, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Pages.AnyAsync(p => p.Slug == slug && (excludeId == null || p.Id != excludeId), cancellationToken);
    }

    public async Task<BlogPage> SavePageAsync(BlogPage page, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        page.UpdatedAtUtc = DateTime.UtcNow;
        if (page.Id == 0)
        {
            page.CreatedAtUtc = DateTime.UtcNow;
            db.Pages.Add(page);
        }
        else
        {
            db.Pages.Update(page);
        }

        await db.SaveChangesAsync(cancellationToken);
        return page;
    }

    public async Task DeletePageAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var page = await db.Pages.FindAsync([id], cancellationToken);
        if (page is not null)
        {
            db.Pages.Remove(page);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> GetPublishedCountAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Posts.CountAsync(p => p.Status == PostStatus.Published, cancellationToken);
    }
}
