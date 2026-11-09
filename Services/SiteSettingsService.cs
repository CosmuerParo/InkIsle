using InkIsle.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace InkIsle.Services;

public sealed class SiteSettingsService(IDbContextFactory<ApplicationDbContext> dbFactory, IMemoryCache cache)
{
    private const string CacheKey = "site-settings-v1";

    public async Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<SiteSettings>(CacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var settings = await db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, cancellationToken);
        if (settings is null)
        {
            settings = new SiteSettings();
            db.SiteSettings.Add(settings);
            await db.SaveChangesAsync(cancellationToken);
        }

        cache.Set(CacheKey, settings, TimeSpan.FromMinutes(5));
        return settings;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
