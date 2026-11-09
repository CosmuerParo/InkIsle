(function () {
    'use strict';

    function wrapSelection(id, before, after, placeholder) {
        const el = document.getElementById(id);
        if (!el) return '';

        const start = el.selectionStart ?? el.value.length;
        const end = el.selectionEnd ?? el.value.length;
        const selected = el.value.slice(start, end) || placeholder || '';
        const next = `${before}${selected}${after}`;
        el.value = el.value.slice(0, start) + next + el.value.slice(end);

        const cursor = start + before.length + selected.length;
        el.focus();
        el.setSelectionRange(cursor, cursor);

        el.dispatchEvent(new Event('input', { bubbles: true }));
        el.dispatchEvent(new Event('change', { bubbles: true }));
        return el.value;
    }

    window.inkEditor = { wrapSelection };
})();
