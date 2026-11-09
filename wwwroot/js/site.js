(() => {
    'use strict';

    const doc = document;
    const root = doc.documentElement;

    const decodeBase64 = (value) => {
        if (!value) return '';
        const binary = atob(value);
        const bytes = Uint8Array.from(binary, c => c.charCodeAt(0));
        return new TextDecoder('utf-8').decode(bytes);
    };

    const processedAssets = new Set();

    function clearCustomAssets() {
        doc.querySelectorAll('style[data-custom-asset]').forEach(el => el.remove());
        doc.querySelectorAll('script[data-custom-script]').forEach(el => el.remove());
        processedAssets.clear();
    }

    function runCustomAssets() {
        doc.querySelectorAll('[data-custom-assets]').forEach(holder => {
            const token = holder.dataset.token;
            if (!token || processedAssets.has(token)) return;
            processedAssets.add(token);

            const id = holder.id || 'custom-assets';
            const css = decodeBase64(holder.dataset.css || '');
            const js = decodeBase64(holder.dataset.js || '');

            if (css) {
                const oldStyle = doc.getElementById(`${id}-style`);
                if (oldStyle) oldStyle.remove();
                const style = doc.createElement('style');
                style.id = `${id}-style`;
                style.dataset.customAsset = id;
                style.textContent = css;
                doc.head.appendChild(style);
            }

            if (js) {
                doc.querySelectorAll(`script[data-custom-script="${id}"]`).forEach(el => el.remove());
                const script = doc.createElement('script');
                script.dataset.customScript = id;
                script.textContent = js;
                doc.body.appendChild(script);
            }
        });
    }

    function initTheme() {
        const button = doc.querySelector('[data-theme-toggle]');
        if (!button || button.dataset.themeBound === 'true') return;
        button.dataset.themeBound = 'true';

        const updateMeta = () => {
            const meta = doc.querySelector('meta[name="theme-color"]');
            if (meta) meta.content = root.dataset.theme === 'dark' ? '#151714' : '#f5f1e8';
        };

        updateMeta();
        button.addEventListener('click', () => {
            const next = root.dataset.theme === 'dark' ? 'light' : 'dark';
            root.dataset.theme = next;
            try { localStorage.setItem('inkisle-theme', next); } catch { }
            updateMeta();
        });
    }

    function initHeader() {
        const header = doc.querySelector('[data-site-header]');
        if (!header) return;

        const update = () => {
            const current = doc.querySelector('[data-site-header]');
            if (current) current.classList.toggle('is-scrolled', window.scrollY > 24);
        };

        if (!window.__inkHeaderBound) {
            window.__inkHeaderBound = true;
            window.addEventListener('scroll', update, { passive: true });
        }

        update();
    }

    function initProgress() {
        const update = () => {
            const bar = doc.querySelector('[data-reading-progress]');
            const article = doc.querySelector('.article, .standalone-page');
            if (!bar) return;

            if (!article) {
                bar.style.display = 'none';
                return;
            }

            bar.style.display = '';
            const rect = article.getBoundingClientRect();
            const total = article.offsetHeight - window.innerHeight;
            const scrolled = Math.min(Math.max(-rect.top, 0), Math.max(total, 1));
            bar.style.transform = `scaleX(${total > 0 ? scrolled / total : 0})`;
        };

        if (!window.__inkProgressBound) {
            window.__inkProgressBound = true;
            window.addEventListener('scroll', update, { passive: true });
            window.addEventListener('resize', update);
        }

        update();
    }

    function initMobileMenu() {
        const button = doc.querySelector('[data-mobile-menu]');
        const panel = doc.querySelector('[data-mobile-panel]');
        if (!button || !panel || button.dataset.menuBound === 'true') return;
        button.dataset.menuBound = 'true';

        const close = () => {
            panel.classList.remove('is-open');
            button.setAttribute('aria-expanded', 'false');
            panel.setAttribute('aria-hidden', 'true');
            doc.body.classList.remove('menu-open');
        };

        button.addEventListener('click', () => {
            const open = !panel.classList.contains('is-open');
            panel.classList.toggle('is-open', open);
            button.setAttribute('aria-expanded', String(open));
            panel.setAttribute('aria-hidden', String(!open));
            doc.body.classList.toggle('menu-open', open);
        });

        panel.querySelectorAll('a').forEach(link => link.addEventListener('click', close));
        window.addEventListener('resize', () => {
            if (window.innerWidth > 980) close();
        });
    }

    function initSearch() {
        const overlay = doc.querySelector('[data-search-overlay]');
        if (!overlay || overlay.dataset.searchBound === 'true') return;
        overlay.dataset.searchBound = 'true';

        const input = overlay.querySelector('[data-search-input]');

        const open = () => {
            overlay.hidden = false;
            requestAnimationFrame(() => {
                overlay.classList.add('is-open');
                doc.body.classList.add('search-open');
                if (input) {
                    input.value = '';
                    setTimeout(() => input.focus(), 120);
                }
            });
        };

        const close = () => {
            overlay.classList.remove('is-open');
            doc.body.classList.remove('search-open');
            setTimeout(() => { overlay.hidden = true; }, 220);
        };

        doc.querySelectorAll('[data-search-open]').forEach(button => {
            button.addEventListener('click', open);
        });
        overlay.querySelectorAll('[data-search-close]').forEach(button => {
            button.addEventListener('click', close);
        });
        overlay.addEventListener('click', event => {
            if (event.target === overlay) close();
        });

        doc.addEventListener('keydown', event => {
            if (event.key === 'Escape' && !overlay.hidden) close();
            if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
                event.preventDefault();
                open();
            }
        });
    }

    function initBackToTop() {
        const update = () => {
            const main = doc.querySelector('[data-back-to-top].back-to-top');
            if (main) main.classList.toggle('is-visible', window.scrollY > 700);
        };

        if (!window.__inkBackToTopBound) {
            window.__inkBackToTopBound = true;
            window.addEventListener('scroll', update, { passive: true });
            doc.addEventListener('click', event => {
                if (event.target.closest('[data-back-to-top]')) {
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        }

        update();
    }

    function initCopyLink() {
        doc.querySelectorAll('[data-copy-link]').forEach(button => {
            if (button.dataset.copyBound === 'true') return;
            button.dataset.copyBound = 'true';
            button.addEventListener('click', async () => {
                const original = button.innerHTML;
                try {
                    await navigator.clipboard.writeText(window.location.href);
                    button.innerHTML = '<svg class="icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m20 6-11 11-5-5"></path></svg> 已复制链接';
                    setTimeout(() => { button.innerHTML = original; }, 2200);
                } catch {
                    button.textContent = '复制失败，请手动复制地址栏';
                }
            });
        });
    }

    function initProse() {
        doc.querySelectorAll('.prose pre').forEach(pre => {
            if (pre.dataset.copyReady === 'true') return;
            pre.dataset.copyReady = 'true';
            const button = doc.createElement('button');
            button.type = 'button';
            button.className = 'code-copy';
            button.textContent = '复制';
            button.addEventListener('click', async () => {
                const code = pre.querySelector('code')?.innerText ?? pre.innerText;
                try {
                    await navigator.clipboard.writeText(code);
                    button.textContent = '已复制';
                    setTimeout(() => { button.textContent = '复制'; }, 1800);
                } catch {
                    button.textContent = '失败';
                }
            });
            pre.appendChild(button);
        });

        doc.querySelectorAll('.prose h2[id], .prose h3[id]').forEach(heading => {
            if (heading.querySelector('.heading-anchor')) return;
            const anchor = doc.createElement('a');
            anchor.href = `#${heading.id}`;
            anchor.className = 'heading-anchor';
            anchor.setAttribute('aria-label', '链接到这一节');
            anchor.textContent = '#';
            heading.appendChild(anchor);
        });
    }

    function initToc() {
        const toc = doc.querySelector('.article-toc');
        if (!toc) return;

        const links = Array.from(toc.querySelectorAll('a[href^="#"]'));
        const headings = links.map(link => doc.getElementById(decodeURIComponent(link.getAttribute('href').slice(1)))).filter(Boolean);
        if (headings.length === 0) return;

        window.__inkTocObserver?.disconnect();
        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                links.forEach(link => link.classList.remove('is-active'));
                const active = links.find(link => decodeURIComponent(link.getAttribute('href').slice(1)) === entry.target.id);
                if (active) active.classList.add('is-active');
            });
        }, { rootMargin: '-20% 0px -68% 0px', threshold: 0 });

        headings.forEach(heading => observer.observe(heading));
        window.__inkTocObserver = observer;
    }

    function initAdminMenu() {
        const button = doc.querySelector('[data-admin-menu]');
        const overlay = doc.querySelector('[data-admin-overlay]');
        if (!button || button.dataset.adminMenuBound === 'true') return;
        button.dataset.adminMenuBound = 'true';

        const close = () => doc.body.classList.remove('admin-sidebar-open');
        button.addEventListener('click', () => doc.body.classList.toggle('admin-sidebar-open'));
        overlay?.addEventListener('click', close);
        doc.querySelectorAll('.admin-nav a').forEach(link => link.addEventListener('click', close));
    }

    function setup() {
        initTheme();
        initHeader();
        initProgress();
        initMobileMenu();
        initSearch();
        initBackToTop();
        initCopyLink();
        initProse();
        initToc();
        initAdminMenu();
        runCustomAssets();
    }

    if (doc.readyState === 'loading') {
        doc.addEventListener('DOMContentLoaded', setup);
    } else {
        setup();
    }

    doc.addEventListener('enhancedload', () => {
        clearCustomAssets();
        setup();
    });
})();
