using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InkIsle.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BlogPost> Posts => Set<BlogPost>();

    public DbSet<BlogPage> Pages => Set<BlogPage>();

    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BlogPost>(entity =>
        {
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasIndex(p => p.Status);
            entity.HasIndex(p => p.PublishedAtUtc);
            entity.Property(p => p.Status).HasConversion<int>();
        });

        builder.Entity<BlogPage>(entity =>
        {
            entity.HasIndex(p => p.Slug).IsUnique();
        });

        builder.Entity<SiteSettings>(entity =>
        {
            entity.HasKey(s => s.Id);
        });
    }
}
