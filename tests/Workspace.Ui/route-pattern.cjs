// UI waits accept both the feature URLs and the legacy bookmarks.
const actions = {
    '/Account/Login': '/TaiKhoan/DangNhap',
    '/Workspace/Profile': '/HoSoGiaSu/ChiTiet',
    '/Workspace/Need': '/NhuCau/ChiTiet',
    '/Workspace/Needs': '/NhuCau/Index',
    '/Workspace/Connections': '/KetNoi/DanhSach',
    '/Workspace/Propose': '/KetNoi/GuiDeXuat',
    '/Workspace/Messages': '/TinNhan/DanhSachHoiThoai'
};
module.exports = (url, pattern) => {
    const value = url.toString();
    const glob = p => new RegExp('^' + p.replace(/[.+?^${}()|[\]\\]/g, '\\$&').replaceAll('*', '.*') + '$').test(value);
    if (glob(pattern)) return true;
    if (pattern === '**/Workspace') return glob('**/Workspace/Index');
    for (const [old, next] of Object.entries(actions)) {
        if (!pattern.startsWith('**' + old) || !['','/','#'].includes(pattern.slice(old.length + 2, old.length + 3))) continue;
        let suffix = pattern.slice(old.length + 2);
        if (suffix === '/**') suffix = '*';
        return glob('**' + next + suffix);
    }
    return false;
};
