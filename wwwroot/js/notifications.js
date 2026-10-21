'use strict';
(() => {
    document.querySelectorAll('[data-flash]').forEach(notice => {
        const dismiss = () => {
            clearTimeout(timer);
            const region = notice.parentElement;
            notice.remove();
            if (region && !region.querySelector('[data-flash]')) region.remove();
        };
        const timer = setTimeout(dismiss, 5000);
        notice.querySelector('[data-flash-close]')?.addEventListener('click', dismiss);
    });
})();
