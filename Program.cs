using System.Text;
using System.Xml.Linq;
using InkIsle.Components;
using InkIsle.Components.Account;
using InkIsle.Data;
using InkIsle.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var contentRoot = builder.Environment.ContentRootPath;
var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=App_Data/inkisle.db";
var sqliteBuilder = new SqliteConnectionStringBuilder(configuredConnection);
if (!Path.IsPathRooted(sqliteBuilder.DataSource))
{
    sqliteBuilder.DataSource = Path.Combine(contentRoot, sqliteBuilder.DataSource);
}

var databaseDirectory = Path.GetDirectoryName(sqliteBuilder.DataSource);
if (!string.IsNullOrWhiteSpace(databaseDirectory))
{
    Directory.CreateDirectory(databaseDirectory);
}

var connectionString = sqliteBuilder.ToString();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

var keysDirectory = Path.Combine(databaseDirectory ?? Path.Combine(contentRoot, "App_Data"), "keys");
Directory.CreateDirectory(keysDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("InkIsle");

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddScoped<BlogService>();
builder.Services.AddScoped<SiteSettingsService>();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 6;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "inkisle.auth";
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    await next();
});

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

var faviconPath = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(contentRoot, "wwwroot"), "images", "favicon.ico");
app.MapGet("/favicon.ico", () => Results.File(faviconPath, "image/x-icon"));

app.MapGet("/feed.xml", async (HttpContext context, BlogService blog, SiteSettingsService settingsService, CancellationToken cancellationToken) =>
{
    var settings = await settingsService.GetAsync(cancellationToken);
    var posts = await blog.GetFeedAsync(30, cancellationToken);
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

    var atom = XNamespace.Get("http://www.w3.org/2005/Atom");
    var channel = new XElement("channel",
        new XElement("title", settings.SiteTitle),
        new XElement("link", baseUrl),
        new XElement("description", settings.Description),
        new XElement("language", "zh-CN"),
        new XElement("lastBuildDate", DateTimeOffset.UtcNow.ToString("R")),
        new XElement(atom + "link",
            new XAttribute("href", $"{baseUrl}/feed.xml"),
            new XAttribute("rel", "self"),
            new XAttribute("type", "application/rss+xml")));

    foreach (var post in posts)
    {
        var url = $"{baseUrl}/blog/{post.Slug}";
        channel.Add(new XElement("item",
            new XElement("title", post.Title),
            new XElement("link", url),
            new XElement("guid", new XAttribute("isPermaLink", "true"), url),
            new XElement("description", post.Summary),
            new XElement("pubDate", (post.PublishedAtUtc ?? post.CreatedAtUtc).ToString("R")),
            new XElement("category", post.Tags)));
    }

    var document = new XDocument(new XDeclaration("1.0", "utf-8", null),
        new XElement("rss", new XAttribute("version", "2.0"), channel));
    return Results.Text(document.ToString(), "application/rss+xml; charset=utf-8", Encoding.UTF8);
});

app.MapGet("/sitemap.xml", async (HttpContext context, BlogService blog, CancellationToken cancellationToken) =>
{
    var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
    var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
    var urls = new List<XElement>
    {
        new(ns + "url", new XElement(ns + "loc", baseUrl), new XElement(ns + "changefreq", "daily")),
        new(ns + "url", new XElement(ns + "loc", $"{baseUrl}/blog"), new XElement(ns + "changefreq", "daily")),
        new(ns + "url", new XElement(ns + "loc", $"{baseUrl}/archive"), new XElement(ns + "changefreq", "weekly"))
    };

    foreach (var post in await blog.GetFeedAsync(200, cancellationToken))
    {
        urls.Add(new XElement(ns + "url",
            new XElement(ns + "loc", $"{baseUrl}/blog/{post.Slug}"),
            new XElement(ns + "lastmod", (post.UpdatedAtUtc).ToString("yyyy-MM-dd")),
            new XElement(ns + "changefreq", "monthly")));
    }

    foreach (var page in await blog.GetPublishedPagesAsync(cancellationToken))
    {
        urls.Add(new XElement(ns + "url",
            new XElement(ns + "loc", $"{baseUrl}/{page.Slug}"),
            new XElement(ns + "lastmod", page.UpdatedAtUtc.ToString("yyyy-MM-dd")),
            new XElement(ns + "changefreq", "monthly")));
    }

    var document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(ns + "urlset", urls));
    return Results.Text(document.ToString(), "application/xml; charset=utf-8", Encoding.UTF8);
});

app.MapGet("/robots.txt", (HttpContext context) => Results.Text($"""
    User-agent: *
    Allow: /
    Disallow: /admin
    Disallow: /account
    Disallow: /api

    Sitemap: {context.Request.Scheme}://{context.Request.Host}/sitemap.xml
    """, "text/plain; charset=utf-8"));

await SeedData.InitializeAsync(app.Services, app.Configuration, app.Environment, app.Logger);
await app.RunAsync();
