'use strict';
(() => {
    const nav = document.getElementById('workspace-nav');
    if (nav && window.matchMedia('(max-width: 767px)').matches) nav.classList.remove('show');
    const region = document.getElementById('need-region');
    const format = document.getElementById('need-format');
    if (region && format) {
        const update = () => {
            region.required = format.value === '1'; region.disabled = format.value === '0';
            const address = document.getElementById('need-address');
            if (address) address.disabled = format.value === '0';
            document.getElementById('need-address-group')?.classList.toggle('d-none', format.value === '0');
        };
        format.addEventListener('change', update); update();
    }
    const compareInputs = document.querySelectorAll('[data-compare]');
    const tray = document.getElementById('compare-tray');
    const selection = new Map();
    const canCompare = document.body.dataset.screen === 'Tim';
    const storageKey = 'educonnect-comparison-v2:' + document.body.dataset.accountId;
    try {
        const saved = JSON.parse(sessionStorage.getItem(storageKey) || '[]');
        if (Array.isArray(saved)) saved.slice(0,3).forEach(item => { if (item && typeof item.id === 'string') selection.set(item.id,item); });
    } catch { /* A browser may disable storage; comparison still works on this page. */ }
    const update = () => {
        tray?.classList.toggle('d-none', !canCompare || selection.size === 0);
        const count = document.getElementById('compare-count');
        if (count) count.textContent = `Đã chọn ${selection.size}/3`;
        const open = document.getElementById('open-comparison');
        if (open) open.disabled = selection.size < 2;
        try { sessionStorage.setItem(storageKey,JSON.stringify([...selection.values()])); } catch { }
    };
    compareInputs.forEach(input => input.addEventListener('change', () => {
        if (input.checked && selection.size >= 3) {
            input.checked = false;
            document.querySelector('.compare-limit')?.remove();
            const message = document.createElement('div'); message.className = 'compare-limit'; message.setAttribute('role','status');
            message.textContent = 'Bạn chỉ có thể so sánh tối đa 3 nhu cầu.'; document.body.append(message);
            setTimeout(() => message.remove(), 4000); return;
        }
        if (input.checked) selection.set(input.dataset.id, {...input.dataset}); else selection.delete(input.dataset.id);
        update();
    }));
    compareInputs.forEach(input => { input.checked = selection.has(input.dataset.id); });
    update();
    document.getElementById('clear-comparison')?.addEventListener('click', () => {
        selection.clear(); compareInputs.forEach(input => { input.checked = false; }); update();
    });
    document.getElementById('open-comparison')?.addEventListener('click', () => {
        const container = document.getElementById('comparison-content'); container.replaceChildren();
        const intro = document.createElement('p'); intro.className = 'comparison-intro';
        intro.textContent = 'Đặt các nhu cầu cạnh nhau để chọn điều kiện phù hợp với bạn.'; container.append(intro);
        const scroll = document.createElement('div'); scroll.className = 'comparison-scroll'; scroll.tabIndex = 0;
        scroll.setAttribute('role','region'); scroll.setAttribute('aria-label','Bảng so sánh, cuộn ngang để xem đầy đủ trên điện thoại');
        const table = document.createElement('table'); table.className = 'comparison-table';
        table.style.setProperty('--comparison-count', selection.size);
        const caption = document.createElement('caption'); caption.textContent = 'Thông tin do người đăng cung cấp. Bảng đối chiếu chưa tính điểm phù hợp.'; table.append(caption);
        const head = document.createElement('thead'), header = document.createElement('tr');
        const criteria = document.createElement('th'); criteria.scope='col'; criteria.className='comparison-label'; criteria.textContent='Tiêu chí'; header.append(criteria);
        let index=0;
        selection.forEach(item => {
            const cell=document.createElement('th');cell.scope='col';
            const number=document.createElement('span');number.className='comparison-number';number.textContent='LỰA CHỌN '+(++index);
            const avatar=document.createElement('span');avatar.className='avatar';avatar.textContent=(item.name||'?').trim().split(/\s+/).pop().slice(0,1);
            const name=document.createElement('strong');name.className='comparison-name';name.textContent=item.name||'Người đăng';
            cell.append(number,avatar,name);header.append(cell);
        });head.append(header);table.append(head);
        const body = document.createElement('tbody');
        const rows = [['subject','Môn học'],['grade','Khối lớp'],['format','Hình thức'],['region','Khu vực'],['fee','Học phí'],['weekly','Lịch học']];
        rows.forEach(([key,label]) => {
            const tr=document.createElement('tr'); const th=document.createElement('th'); th.scope='row'; th.textContent=label;tr.append(th);
            if(key==='fee')tr.className='comparison-price';
            selection.forEach(item=>{const td=document.createElement('td');td.textContent=item[key]||'Chưa công bố';tr.append(td);});body.append(tr);
        }); table.append(body);scroll.append(table);container.append(scroll);
    });
    document.querySelectorAll('[data-workspace-form]').forEach(form => form.addEventListener('submit', event => {
        if (!form.checkValidity()) return;
        const button=event.submitter;
        if (button) { button.setAttribute('aria-busy','true'); button.textContent='Đang lưu…'; }
        // Do not disable submit buttons: their name/value determines Draft vs Open.
    }));
})();
