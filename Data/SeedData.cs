using InkIsle.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InkIsle.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@inkisle.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "InkIsle@2026";
        var adminDisplayName = configuration["Seed:AdminDisplayName"] ?? "墨屿主人";
        var admin = await userManager.FindByEmailAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = adminDisplayName
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
            {
                logger.LogError("Unable to seed administrator: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            }
            else
            {
                logger.LogInformation("Seeded administrator {Email}. Change the default password after first login.", adminEmail);
            }
        }

        if (admin is not null && !await userManager.IsInRoleAsync(admin, "Admin"))
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }

        if (!await db.SiteSettings.AnyAsync(cancellationToken))
        {
            db.SiteSettings.Add(new SiteSettings
            {
                SiteTitle = "墨屿",
                Tagline = "在文字的岛屿上，慢一点。",
                Description = "墨屿是一个关于写作、技术、阅读与生活的独立博客。记录值得反复阅读的思考，也收藏代码与日常的微光。",
                HeroKicker = "A QUIET PLACE FOR IDEAS",
                HeroTitle = "在文字的岛屿上，慢一点。",
                HeroSubtitle = "记录技术、阅读与日常的微光。这里没有信息瀑布，只有值得反复阅读的句子。",
                AuthorName = "墨屿主人",
                AuthorBio = "写代码，也写字。全栈开发者，长期主义的实践者。相信好的工具应该安静地工作，好的文字应该慢慢地抵达。",
                FooterText = "愿每一次抵达，都有文字留下回声。",
                Email = "hello@inkisle.local",
                GithubUrl = "https://github.com/",
                AccentColor = "#c65a3f",
                SiteWideCss = "",
                SiteWideJs = ""
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Posts.AnyAsync(cancellationToken))
        {
            var now = DateTime.UtcNow;
            db.Posts.AddRange(
                BuildSlowPost(now.AddDays(-2)),
                BuildJavaScriptPost(now.AddDays(-9)),
                BuildDotNetPost(now.AddDays(-18)),
                BuildReadingPost(now.AddDays(-34)),
                BuildDraftPost(now));
            await db.SaveChangesAsync(cancellationToken);
        }

        if (!await db.Pages.AnyAsync(cancellationToken))
        {
            db.Pages.AddRange(
                new BlogPage
                {
                    Title = "关于",
                    Slug = "about",
                    Summary = "关于墨屿，以及一个写字的人。",
                    ShowInNavigation = true,
                    SortOrder = 1,
                    IsPublished = true,
                    ContentMarkdown = """
                        你好，欢迎来到**墨屿**。

                        这是一个不追热点的独立博客。它建在一座虚构的小岛上，用来安放那些需要多一点时间才能说完的话：关于代码与手艺，关于阅读与生活，也关于一个人如何与自己相处。

                        ## 为什么叫墨屿

                        「墨」是文字，「屿」是被水围住的一小块陆地。互联网像海，信息像潮水；我希望这里是一块可以停靠的岛屿，而不是另一条湍急的河流。

                        岛不大，但足够散步。

                        ## 关于我

                        我是一名全栈开发者，主要和 C#、.NET、前端工程以及一点点设计打交道。白天写能运行的代码，晚上写不一定有人看的字。

                        - 相信长期主义，也接受偶尔的停滞
                        - 喜欢简单的工具、清晰的接口和留白的页面
                        - 正在练习：把复杂的事情说简单

                        ## 关于这个站点

                        站点使用 **.NET 8 Blazor Web App** 构建，数据存放在 **SQLite**，后台可以完成文章、独立页面的编辑与发布。每篇文章都支持自定义 CSS 与 JavaScript，用来放一些可交互的小实验。

                        如果你也想搭一个自己的角落，可以从这里开始。

                        > 写作不是把世界解释清楚，而是给自己一个继续观察的理由。

                        你可以在任何一篇文章下面找到我的联系方式，或者沿着页脚的 RSS 订阅这里缓慢的更新。
                        """
                },
                new BlogPage
                {
                    Title = "友链",
                    Slug = "links",
                    Summary = "一些值得常去的小岛。",
                    ShowInNavigation = true,
                    SortOrder = 2,
                    IsPublished = true,
                    ContentMarkdown = """
                        互联网上仍然有一些安静、认真、持续更新的地方。这里记录我常去的几座小岛。

                        | 名字 | 关于 | 链接 |
                        | --- | --- | --- |
                        | .NET Blog | 官方团队关于 .NET 与 C# 的更新 | [devblogs.microsoft.com/dotnet](https://devblogs.microsoft.com/dotnet/) |
                        | Blazor 文档 | 从入门到生产实践的官方文档 | [learn.microsoft.com/aspnet/core/blazor](https://learn.microsoft.com/aspnet/core/blazor/) |
                        | Markdig | 这个站点使用的 Markdown 引擎 | [github.com/xoofx/markdig](https://github.com/xoofx/markdig) |

                        ## 交换友链

                        如果你也在认真写东西，欢迎来信。请附上站点名称、一句介绍和地址。我更喜欢更新不频繁但每篇都值得读完的博客。

                        > 慢一点没关系，持续就好。
                        """
                },
                new BlogPage
                {
                    Title = "写作约定",
                    Slug = "writing-principles",
                    Summary = "这个博客如何处理事实、观点与代码。",
                    ShowInNavigation = false,
                    SortOrder = 9,
                    IsPublished = true,
                    ContentMarkdown = """
                        为了让这里的内容更可靠，我给自己定了几条简单的约定。

                        1. **区分事实与观点。** 技术细节尽量给出验证方式，个人判断尽量说明前提。
                        2. **代码必须能运行。** 示例优先来自真实项目，并注明版本与环境。
                        3. **允许修正。** 文章会持续更新，重要修改会在文末备注。
                        4. **不追求日更。** 与其制造内容，不如等待一个值得写下来的想法。

                        如果你发现错误，欢迎告诉我。认真指出错误的人，是写作者的朋友。
                        """
                });
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("InkIsle database ready at {Environment}.", environment.EnvironmentName);
    }

    private static BlogPost BuildSlowPost(DateTime published) => new()
    {
        Title = "在文字的岛屿上，慢一点",
        Slug = "slow-writing-on-the-island",
        Summary = "当信息越来越多，注意力越来越碎，我们为什么还需要一个更新很慢的博客？",
        Tags = "写作, 生活, 思考",
        CoverImageUrl = "/images/cover-slow.svg",
        Status = PostStatus.Published,
        IsFeatured = true,
        PublishedAtUtc = published,
        AuthorName = "墨屿主人",
        ContentMarkdown = """
            我重新开始写博客，是在一个很普通的晚上。

            那天我关掉了所有推送，把手机放在另一个房间，然后打开一个空白文档。光标闪了很久，我发现自己已经不太习惯这种没有提示、没有进度条、也没有人催更的写作了。

            我们被训练得太擅长「快速回应」：一条消息要在几秒内回复，一个观点要在几十个字内说完，一篇文章最好配好标题、封面和三条金句。可真正值得写下来的东西，往往长得更慢。

            ## 一个博客应该是什么

            它不必是个人品牌的橱窗，也不必是知识付费的入口。它可以只是一块属于自己的地方：你把一段时间里的观察放在这里，过路的人愿意停下来读一读，就很好了。

            我喜欢把博客想象成一座岛：

            - 它不追逐潮水，但知道潮水会来
            - 它有自己的地形，不必和别人的岛一样
            - 它欢迎抵达，也接受离开
            - 它最大的价值，是允许你按照自己的速度生长

            > 好的文字不是信息密度最高的文字，而是让人愿意多停留一会儿的文字。

            ## 慢，不是低效

            「慢」并不意味着停止。它更接近于一种有意识的节奏：先观察，再理解，最后才表达。

            一篇文章可能需要两周才能写完。期间我会删掉一半的句子，查证几个链接，把一段过于顺滑的结论重新拆开。这个过程不会出现在时间线上，但它决定了文章最后能不能站稳。

            技术写作尤其如此。代码跑通只是开始，真正困难的是解释：为什么这样设计？边界在哪里？什么时候不该用它？

            ## 给同样想写的人

            如果你也想拥有一个自己的角落，我有三个不太成熟的建议：

            1. **先写，再装修。** 不要等到域名、主题、字体都完美了才开始，第一篇文章可以很朴素。
            2. **写你真正想知道的事。** 不用替所有人总结，先替未来的自己留下记录。
            3. **允许它不完整。** 一篇诚实的、有缺口文章，比一篇圆滑但空洞的文章更值得读。

            这个博客会更新得很慢。但每一次更新，我都希望它值得你花几分钟。

            *写于一个起风的夜晚。*
            """
    };

    private static BlogPost BuildJavaScriptPost(DateTime published) => new()
    {
        Title = "在博客里，给一篇文章加一点 JavaScript",
        Slug = "embed-javascript-in-blog",
        Summary = "不是所有文章都需要交互，但有些想法用一段可点击的代码来解释，会更容易抵达。",
        Tags = "JavaScript, Blazor, 教程",
        CoverImageUrl = "/images/cover-js.svg",
        Status = PostStatus.Published,
        IsFeatured = false,
        PublishedAtUtc = published,
        AuthorName = "墨屿主人",
        ContentMarkdown = """
            静态博客有一个天然的限制：文章是「死」的。它擅长陈述，却不太擅长让人动手试一试。

            所以这个站点给每篇文章留了一个小小的后门：**自定义 JavaScript**。管理员可以在后台为单篇文章附加一段脚本，它在文章页面加载完成后执行。

            下面就是一个可以直接点击的现场演示。它由本文的「自定义 JS」驱动，而不是硬编码在站点里。

            <div class="js-demo" id="js-live-demo">
              <span class="js-demo-label">LIVE DEMO · 文章内嵌 JS</span>
              <p>这段交互来自这篇文章自己的脚本区域。按钮、状态和颜色变化都由它控制。</p>
              <button type="button" class="btn-demo">点我一下</button>
              <output data-output>等待你的第一次点击</output>
            </div>

            ## 它是怎么工作的

            后台编辑器里有一个「自定义 JavaScript」折叠面板。发布时，脚本会和文章一起存入 SQLite。公开页面渲染时，站点不会把脚本直接拼进 HTML，而是把它编码后交给一个统一的小型运行器：

            ```js
            // 简化后的执行流程
            const code = decodeBase64(holder.dataset.js);
            const script = document.createElement('script');
            script.textContent = code;
            document.body.appendChild(script);
            ```

            这样做有两个好处：

            1. 脚本中的引号、换行和特殊字符不会破坏服务端模板；
            2. 兼容 Blazor 的增强导航（enhanced navigation），每次页面内容更新后都能重新执行。

            ## 一段完整的示例

            下面是演示按钮背后的代码。它先检查目标节点是否存在，避免在别的页面报错：

            ```js
            (() => {
              const panel = document.getElementById('js-live-demo');
              if (!panel) return;

              const button = panel.querySelector('button');
              const output = panel.querySelector('[data-output]');
              let count = 0;

              button.addEventListener('click', () => {
                count += 1;
                output.textContent = `你好，第 ${count} 次点击 ✦`;
                panel.style.setProperty('--demo-glow', 0.2 + count * 0.08);
              });
            })();
            ```

            ## 一些边界

            自定义脚本只应该由站点管理员添加。它拥有和页面相同的权限，可以读取 DOM、发起请求，也可以写 `localStorage`。把它开放给访客，等同于把站点交给陌生人。

            如果脚本要操作页面节点，记得：

            - 用 `if (!node) return;` 做存在性检查
            - 监听器尽量挂在文章内部的元素上
            - 不要假设它只会执行一次，导航后可能需要重新绑定

            交互不是目的。它只是当文字不够用的时候，另一种解释方式。

            > 最好的交互，是让人忘记交互本身，只记住那个被解释清楚的想法。
            """,
        CustomJs = """
            (() => {
              const panel = document.getElementById('js-live-demo');
              if (!panel) return;

              const button = panel.querySelector('button');
              const output = panel.querySelector('[data-output]');
              let count = 0;

              button.addEventListener('click', () => {
                count += 1;
                output.textContent = `你好，第 ${count} 次点击 ✦`;
                panel.style.setProperty('--demo-glow', 0.2 + count * 0.08);
              });
            })();
            """
    };

    private static BlogPost BuildDotNetPost(DateTime published) => new()
    {
        Title = "用 .NET 8 + Blazor + SQLite，搭一个自己的写作系统",
        Slug = "dotnet8-blazor-sqlite-blog",
        Summary = "从一篇 Markdown 到一座可以自己维护的岛：这个博客的技术选型与实现笔记。",
        Tags = ".NET, Blazor, SQLite",
        CoverImageUrl = "/images/cover-dotnet.svg",
        Status = PostStatus.Published,
        IsFeatured = false,
        PublishedAtUtc = published,
        AuthorName = "墨屿主人",
        ContentMarkdown = """
            这个站点没有使用现成的博客框架，而是用 **.NET 8 Blazor Web App** 从零搭建。原因很简单：我想要一个足够小、可以完全理解的系统。

            ## 技术栈

            | 层 | 选择 | 理由 |
            | --- | --- | --- |
            | Web | Blazor Web App (.NET 8) | 静态 SSR 保证首屏与 SEO，后台用 Interactive Server |
            | 数据 | EF Core 8 + SQLite | 单文件、零运维，备份就是复制文件 |
            | 认证 | ASP.NET Core Identity | 成熟的 Cookie 认证与角色授权 |
            | 内容 | Markdown + Markdig | 写作时专注文字，渲染时保持可控 |
            | UI | 原生 CSS + 少量 JS | 没有前端构建链，减少依赖 |

            ## 为什么是 Blazor Web App

            .NET 8 的 Blazor Web App 提供了一种很舒服的混合模式：公开页面默认使用静态服务端渲染，搜索引擎和首屏体验都很好；后台编辑器则开启 `InteractiveServer`，获得实时表单与预览。

            ```csharp
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();
            ```

            页面级渲染模式让两者可以共存，而不需要维护两套前端。

            ## 数据模型保持简单

            核心只有三张表：

            - `Posts`：文章，包含 Markdown、标签、状态、SEO 与自定义 CSS/JS
            - `Pages`：独立页面，用来放「关于」「友链」这类内容
            - `SiteSettings`：站点名称、首页文案、页脚与全站脚本

            EF Core 的迁移负责建表。启动时，`SeedData` 会确保数据库存在，并写入初始管理员与示例内容：

            ```csharp
            await db.Database.MigrateAsync();

            if (!await db.SiteSettings.AnyAsync())
            {
                db.SiteSettings.Add(new SiteSettings { SiteTitle = "墨屿" });
                await db.SaveChangesAsync();
            }
            ```

            ## 后台编辑器的几个细节

            Markdown 编辑器采用「左边写、右边预览」的分栏。预览不是前端库渲染的，而是直接调用服务端的 Markdig，所以它和最终页面使用完全相同的管线。

            保存时还有几道保险：

            1. Slug 在数据库中有唯一索引；
            2. 空 Slug 会自动从标题生成；
            3. 新文章默认是草稿，只有显式发布才会出现在公开页面；
            4. 所有后台路由都要求 `Admin` 角色。

            ```csharp
            [Authorize(Roles = "Admin")]
            public partial class AdminPageBase : ComponentBase
            {
                // 后台页面的授权入口
            }
            ```

            ## 部署与备份

            SQLite 数据库位于 `App_Data/inkisle.db`。它没有外部服务依赖，备份只需要：

            ```bash
            cp App_Data/inkisle.db backups/inkisle-$(date +%F).db
            ```

            发布时使用 `dotnet publish -c Release`，然后用 systemd、Docker 或任何你熟悉的方式托管即可。

            ## 最后

            自己搭博客最大的收益，不是「拥有一个网站」，而是你被迫重新思考：内容如何组织，页面如何阅读，什么应该被保留，什么可以删掉。

            工具会旧，文章会留下。希望这个小小的系统，能陪我写得久一点。

            > 系统应该像一间安静的书房：你走进去，只想坐下来写字。
            """
    };

    private static BlogPost BuildReadingPost(DateTime published) => new()
    {
        Title = "岛屿书单：最近读过的五本书",
        Slug = "island-reading-list",
        Summary = "关于写作、专注、城市与一点虚构。不是推荐，只是一些阅读留下的痕迹。",
        Tags = "阅读, 书单, 生活",
        CoverImageUrl = "/images/cover-books.svg",
        Status = PostStatus.Published,
        IsFeatured = false,
        PublishedAtUtc = published,
        AuthorName = "墨屿主人",
        ContentMarkdown = """
            读书是很私人的事。同一本书在不同的时候读，会留下完全不同的划痕。

            这份书单没有排名，也不构成推荐。它只是记录最近几个月里，哪些句子还在脑子里回响。

            ## 1. 《一间自己的房间》

            伍尔夫说，一个女人要写小说，必须有钱，还要有一间自己的房间。

            这句话常被引用成关于性别与创作的宣言，但我这次读到的，是「空间」本身的重要性。一个不被打扰的角落，一段不需要解释的时间，对任何写作者来说都不是奢侈品，而是基础设施。

            > 不必匆忙，不必火花四溅，不必成为别人，只需做自己。

            ## 2. 《深度工作》

            这本书的方法论并不新鲜，但它提醒了我一件事：专注不是一种意愿，而是一种需要被保护的状态。

            我开始把手机放在另一个房间，把写作时间切成 90 分钟一段。效果比任何待办事项工具都好。

            ## 3. 《看不见的城市》

            卡尔维诺的城市都不存在，但每一座都像在描述我们住过的地方。

            读它的时候，我在想：一个博客其实也是一座看不见的城市。访客只看到页面，却看不到作者在无数个夜晚删除的段落。

            ## 4. 《写作这回事》

            斯蒂芬·金谈写作，最有用的一句反而很朴素：**多读，多写。**

            没有捷径。所有关于灵感的讨论，最后都要回到桌前。

            ## 5. 《东京八平米》

            一本关于小空间生活的书。作者把租住的八平米房间写成了一个完整的宇宙。

            我喜欢这种尺度：不试图解释世界，只把一个很小的角落写清楚。写博客也应该如此。

            ## 读完以后

            书单很容易变成一种表演，好像读过的书会变成某种身份。但我更愿意把它们当成路标：提醒自己曾经在哪里停下来，想过什么。

            下一份书单，可能要等冬天过去。
            """
    };

    private static BlogPost BuildDraftPost(DateTime now) => new()
    {
        Title = "草稿：2026 年冬季写作计划",
        Slug = "winter-writing-plan-2026",
        Summary = "一些还没有想清楚的写作方向。",
        Tags = "写作, 计划",
        Status = PostStatus.Draft,
        IsFeatured = false,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        AuthorName = "墨屿主人",
        ContentMarkdown = """
            > 这是一篇草稿，只在后台可见。

            ## 想写的题目

            - 为什么我仍然使用 Markdown
            - 一个人的技术栈应该如何生长
            - 从「做完」到「做好」之间
            - 关于注意力的一点观察

            ## 还没想清楚的部分

            这些题目之间似乎有一条共同的线：**如何在一个追求速度的环境里，保留自己的节奏。**

            等想清楚了再写。
            """
    };
}
