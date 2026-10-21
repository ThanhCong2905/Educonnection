// Run against the isolated database/server described in README.md.
const http = require('http');
const assert = require('assert/strict');
const crypto = require('crypto');
const {execFileSync} = require('child_process');
const origin = 'http://localhost:5198';
const decode = s => s.replaceAll('&amp;', '&').replace(/&#x([0-9a-f]+);/gi, (_, n) => String.fromCodePoint(parseInt(n, 16)));
class Session {
    cookies = new Map();
    async request(url, fields, follow = true) {
        const target = new URL(url, origin);
        assert.equal(target.origin, origin);
        const body = fields ? new URLSearchParams(fields).toString() : null;
        const response = await new Promise((resolve, reject) => {
            const req = http.request(target, {method: body ? 'POST' : 'GET', headers: {
                Cookie: [...this.cookies].map(([k,v]) => `${k}=${v}`).join('; '),
                ...(body ? {'Content-Type': 'application/x-www-form-urlencoded', 'Content-Length': Buffer.byteLength(body)} : {})
            }}, res => {
                for (const value of res.headers['set-cookie'] || []) {
                    const pair = value.split(';')[0], split = pair.indexOf('=');
                    this.cookies.set(pair.slice(0, split), pair.slice(split + 1));
                }
                let text = '';res.setEncoding('utf8');res.on('data', chunk => text += chunk);
                res.on('end', () => resolve({status: res.statusCode, html: text, location: res.headers.location, url: target.href}));
            });req.on('error', reject);req.end(body);
        });
        if (follow && response.status >= 300 && response.status < 400 && response.location)
            return this.request(response.location);
        return response;
    }
    async submit(page, action, fields) {
        const token = page.html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);
        assert.ok(token, 'form has antiforgery token');
        return this.request(action, {...fields, __RequestVerificationToken: decode(token[1])});
    }
}
(async () => {
    execFileSync('powershell', ['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/Workspace.Ui/database.ps1','-Action','Verify']);
    const session = new Session(), id = crypto.randomBytes(5).toString('hex');
    let page = await session.request('/TaiKhoan/DangKy');assert.equal(page.status, 200);
    const password = `Architecture ${id} test password`, email = `workspace-test-${id}@example.invalid`;
    page = await session.submit(page, '/TaiKhoan/DangKy', {Role:'PhuHuynh',FullName:'Kiểm thử cấu trúc',Email:email,Phone:'098'+String(parseInt(id.slice(0,6),16)%10000000).padStart(7,'0'),Password:password,ConfirmPassword:password,AcceptTerms:'true'});
    assert.equal(page.status, 200);assert.match(page.html, /name="Identifier"/);
    page = await session.submit(page, '/TaiKhoan/DangNhap', {Identifier:email,Password:password});
    assert.equal(page.status, 200);assert.match(page.html, /workspace-nav/);
    console.log('PASS register and login with Vietnamese controller actions');
    for (const route of ['/Workspace','/TimKiem/Tim','/NhuCau/Index','/HoSoGiaSu/ChiTiet','/KetNoi/DanhSach','/TinNhan/DanhSachHoiThoai','/LopHoc/DanhSach','/Workspace/Search','/Workspace/Needs','/Workspace/Profile','/Workspace/Connections','/Workspace/Messages','/Workspace/Classes','/Account/Overview']) {
        page = await session.request(route);assert.equal(page.status, 200, route);
        assert.ok(!page.html.includes('An unhandled exception'), route);
        for (const [,href] of page.html.matchAll(/<a\b[^>]*href="([^"]+)"/g)) {
            if (!href.startsWith('/') || href.startsWith('//')) continue;
            const linked = await session.request(decode(href));assert.ok(linked.status < 400, `${route} -> ${href}: ${linked.status}`);
        }
    }
    console.log('PASS new routes, legacy URLs, shared views and navigation links');
    page = await session.request('/HoSoGiaSu/ChiTiet');
    page = await session.submit(page, '/ProfileData/Address', {'Form.City':'Thành phố thử','Form.District':'Quận thử','Form.DetailedAddress':'Địa chỉ kiểm thử'});
    assert.equal(page.status, 200);
    page = await session.request('/NhuCau/Sua');assert.equal(page.status, 200);
    const select = page.html.match(/<select[^>]*name="Form.SubjectId"[^>]*>([\s\S]*?)<\/select>/);
    assert.ok(select);const subject = select[1].match(/value="([1-9]\d*)"/);assert.ok(subject);
    assert.match(page.html, /action="\/NhuCau\/TaoNhuCauHoc"/);
    page = await session.submit(page, '/NhuCau/TaoNhuCauHoc', {'Form.SubjectId':subject[1],'Form.Grade':'Lớp 9','Form.Goal':'Nhu cầu thử kiến trúc','Form.Format':'0','Form.Fee':'200000','Form.FeeUnit':'1','Form.WeeklySessions':'1','Form.LearnerName':'Học sinh thử','Form.Days':'1','Form.Start':'18:00','Form.End':'19:30','Form.Publish':'true'});
    assert.equal(page.status, 200);assert.match(decode(page.html), /Nhu cầu thử kiến trúc/);
    const edit = page.html.match(/href="([^"]*(?:Sua|EditNeed)[^"]+)"/);assert.ok(edit);
    page = await session.request(decode(edit[1]));assert.equal(page.status, 200);assert.match(page.html, /name="Form.Version"/);
    const editUrl = new URL(decode(edit[1]), origin);
    const needId = editUrl.searchParams.get('id') || editUrl.pathname.match(/[0-9a-f-]{36}$/i)?.[0];assert.ok(needId);
    assert.equal((await session.request('/Workspace/EditNeed/' + needId)).status, 200);
    assert.equal((await session.request('/NhuCau/Dong', {id:crypto.randomUUID()}, false)).status,400);
    console.log('PASS need creation, edit links, version form and CSRF protection');
})().catch(error => {console.error(error);process.exitCode = 1;});
