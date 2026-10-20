'use strict';
document.querySelectorAll('[data-password]').forEach(button => {
    button.addEventListener('click', () => {
        const input = document.getElementById(button.dataset.password);
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        button.setAttribute('aria-pressed', String(show));
        button.setAttribute('aria-label', show ? 'Ẩn mật khẩu' : 'Hiện mật khẩu');
    });
});
document.querySelectorAll('[data-confirm]').forEach(input => {
    const password = document.getElementById(input.dataset.confirm);
    const validate = () => input.setCustomValidity(input.value && input.value !== password.value ? 'Mật khẩu xác nhận không khớp.' : '');
    input.addEventListener('input', validate);
    password.addEventListener('input', validate);
});
document.querySelectorAll('[data-auth-form]').forEach(form => {
    form.addEventListener('submit', () => {
        if (!form.checkValidity()) return;
        const button = form.querySelector('button[type="submit"]');
        button.disabled = true;
        button.textContent = button.dataset.busy;
        form.setAttribute('aria-busy', 'true');
    });
});
document.querySelectorAll('[data-retry]').forEach(button => {
    let remaining = Number(button.dataset.retry);
    if (!remaining) return;
    button.disabled = true;
    const render = () => { button.textContent = `Thử lại sau ${remaining} giây`; };
    render();
    const timer = setInterval(() => {
        remaining--;
        if (remaining <= 0) { clearInterval(timer); button.disabled = false; button.textContent = button.dataset.idle; }
        else render();
    }, 1000);
});
window.addEventListener('pageshow', event => {
    if (!event.persisted) return;
    document.querySelectorAll('[data-auth-form]').forEach(form => {
        form.removeAttribute('aria-busy');
        const button = form.querySelector('button[type="submit"]');
        if (button && !Number(button.dataset.retry)) { button.disabled = false; button.textContent = button.dataset.idle; }
    });
});
