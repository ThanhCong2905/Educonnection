const {chromium}=require(process.env.EDU_PLAYWRIGHT||'playwright');
const {execFileSync}=require('child_process');
const fs=require('fs'),crypto=require('crypto'),assert=require('assert/strict');
const origin='http://localhost:5198',suffix=crypto.randomBytes(4).toString('hex'),errors=[];
const output='tmp/tutor-qa';fs.mkdirSync(output,{recursive:true});
const pass=s=>console.log('PASS '+s);
async function register(browser,role,index,admin=false){
 const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage();
 page.on('pageerror',error=>errors.push(error.message));
 page.on('response',response=>{if(response.status()>=500)errors.push(response.url()+' '+response.status());});
 const email=`workspace-test-${suffix}-${index}@example.invalid`,password=`Tutor workflow ${suffix} password`;
 await page.goto(origin+'/TaiKhoan/DangKy');await page.locator(`input[value="${role}"]`).check();
 await page.locator('#FullName').fill('Người thử '+role);await page.locator('#Email').fill(email);
 await page.locator('#Phone').fill('093'+String((parseInt(suffix,16)+index)%10000000).padStart(7,'0'));
 await page.locator('#Password').fill(password);await page.locator('#ConfirmPassword').fill(password);await page.locator('#AcceptTerms').check();
 await page.getByRole('button',{name:'Tạo tài khoản',exact:true}).click();await page.locator('#Identifier').waitFor();
 if(admin)execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/Workspace.Ui/tutor-reviewer.ps1','-Email',email]);
 await page.locator('#Identifier').fill(email);await page.locator('#Password').fill(password);await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();
 if(admin)await page.getByRole('link',{name:'Xét duyệt hồ sơ gia sư',exact:true}).waitFor();else await page.locator('.workspace-nav').waitFor();
 return {context,page,email};
}
async function noOverflow(page,label){assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),label);}
(async()=>{
 execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/Workspace.Ui/database.ps1','-Action','Verify']);
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try{
 const learner=await register(browser,'PhuHuynh',1),tutor=await register(browser,'GiaSu',2),admin=await register(browser,'HocSinh',3,true);
 const name='Gia sư '+suffix;
 await tutor.page.goto(origin+'/HoSoGiaSu/Sua');
 for(const [key,value]of Object.entries({DisplayName:name,School:'Đại học Sư phạm',Major:'Sư phạm Toán',Qualification:'Cử nhân Sư phạm',Expertise:'Toán THCS',Experience:'Kinh nghiệm đã duyệt',Introduction:'Giới thiệu hồ sơ '+suffix,TeachingScope:'Lớp 8, Lớp 9'}))await tutor.page.locator('#Form_'+key).fill(value);
 await tutor.page.locator('#Form_StudyStatus').selectOption('DaTotNghiep');
 await tutor.page.locator('[name="Form.Subjects"]').first().check();
 await tutor.page.locator('#Form_Avatar').setInputFiles({name:'avatar.png',mimeType:'image/png',buffer:Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=','base64')});
 await tutor.page.getByRole('button',{name:'Gửi hồ sơ xét duyệt',exact:true}).click();await tutor.page.getByRole('heading',{name:name,exact:true}).waitFor();
 await tutor.page.locator('#proof-file').setInputFiles({name:'bang-cap.pdf',mimeType:'application/pdf',buffer:Buffer.from('%PDF-1.4\nTutor workflow evidence')});
 await tutor.page.getByRole('button',{name:'Tải minh chứng lên',exact:true}).click();await tutor.page.locator('#proof-file').waitFor();
 await tutor.page.goto(origin+'/HoSoGiaSu/DieuKien');await tutor.page.locator('#Form_Fee').fill('200000');
 await tutor.page.locator('input[type=checkbox][name="Form.Slots[1].Enabled"]').check();await tutor.page.locator('#Form_AcceptingStudents').check();
 await tutor.page.getByRole('button',{name:'Lưu điều kiện nhận lớp',exact:true}).click();await tutor.page.getByRole('heading',{name:name,exact:true}).waitFor();
 await learner.page.goto(origin+'/TimKiem/Tim?Q='+encodeURIComponent(name));assert.equal(await learner.page.locator('.need-card').count(),0);
 await admin.page.goto(origin+'/QuanTri/HoSo');let review=admin.page.locator('.review-profile').filter({has:admin.page.getByRole('heading',{name:name,exact:true})});
 assert.equal(await review.locator('a').count(),1);await review.getByRole('button',{name:'Duyệt hồ sơ',exact:true}).click();await admin.page.getByRole('heading',{name:'Xét duyệt hồ sơ gia sư',exact:true}).waitFor();
 await learner.page.goto(origin+'/TimKiem/Tim?Q='+encodeURIComponent(name));assert.equal(await learner.page.locator('.need-card').count(),1);await learner.page.locator('.need-card a').first().click();
 await learner.page.getByRole('heading',{name:name,exact:true}).waitFor();const profileUrl=learner.page.url();
 assert.equal(await learner.page.locator('input[type=file]').count(),0);assert.ok(!(await learner.page.content()).includes('bang-cap.pdf'));
 const image=await learner.page.locator('.tutor-avatar').first().getAttribute('src');assert.equal((await learner.page.request.get(origin+image)).status(),200);
 assert.equal(await learner.page.locator('.contact-locked fieldset[disabled]').count(),1);assert.ok(await learner.page.getByRole('button',{name:'Gửi yêu cầu kết nối',exact:true}).isDisabled());
 await learner.page.screenshot({path:output+'/contact-before.png',fullPage:true});
 pass('reviewed tutor profile replaces teaching needs; evidence stays private and connection is locked');
 await learner.page.locator('[data-contact-request] textarea').fill('Tôi muốn liên hệ để học Toán.');await learner.page.getByRole('button',{name:'Gửi yêu cầu liên hệ',exact:true}).click();
 await learner.page.getByText('Đang chờ chấp nhận liên hệ',{exact:true}).waitFor();assert.equal(await learner.page.getByRole('button',{name:'Chấp nhận liên hệ',exact:true}).count(),0);
 await tutor.page.goto(origin+'/KetNoi/DanhSach');await tutor.page.getByRole('button',{name:'Chấp nhận liên hệ',exact:true}).click();await tutor.page.getByText('Đã chấp nhận liên hệ',{exact:true}).waitFor();
 await learner.page.goto(profileUrl);assert.ok(await learner.page.getByRole('button',{name:'Gửi yêu cầu kết nối',exact:true}).isEnabled());
 await learner.page.getByRole('button',{name:'Nhắn tin',exact:true}).click();await learner.page.locator('#chat-text').waitFor();
 await tutor.page.goto(origin+'/TinNhan/DanhSachHoiThoai');await tutor.page.locator('.conversation-link').first().click();await tutor.page.locator('#chat-text').waitFor();
 const message='Tin nhắn Enter '+suffix;await learner.page.locator('#chat-text').fill(message);await learner.page.locator('#chat-text').press('Enter');
 await learner.page.locator('#chat-stream').getByText(message,{exact:true}).waitFor();await tutor.page.locator('#chat-stream').getByText(message,{exact:true}).waitFor({timeout:12000});assert.equal(await learner.page.locator('#chat-text').inputValue(),'');
 const count=await learner.page.locator('.chat-message').count();await learner.page.locator('#chat-text').fill('Dòng thứ nhất');await learner.page.locator('#chat-text').press('Shift+Enter');await learner.page.locator('#chat-text').pressSequentially('Dòng thứ hai');
 assert.equal(await learner.page.locator('#chat-text').inputValue(),'Dòng thứ nhất\nDòng thứ hai');assert.equal(await learner.page.locator('.chat-message').count(),count);
 await learner.page.locator('#chat-text').press('Enter');await learner.page.locator('#chat-stream').getByText('Dòng thứ nhất\nDòng thứ hai',{exact:true}).waitFor();
 await learner.page.locator('#chat-text').fill('Đang gõ tiếng Việt');await learner.page.locator('#chat-text').dispatchEvent('keydown',{key:'Enter',isComposing:true});assert.equal(await learner.page.locator('#chat-text').inputValue(),'Đang gõ tiếng Việt');await learner.page.locator('#chat-text').fill('');
 pass('recipient acceptance unlocks chat and connection; Enter sends, Shift+Enter adds a line, IME does not send');
 await tutor.page.goto(origin+'/HoSoGiaSu/DieuKien');await tutor.page.locator('#Form_Fee').fill('300000');await tutor.page.getByRole('button',{name:'Lưu điều kiện nhận lớp',exact:true}).click();
 await learner.page.goto(profileUrl);assert.match(await learner.page.locator('.profile-price').innerText(),/300\.000/);
 await tutor.page.goto(origin+'/HoSoGiaSu/Sua');await tutor.page.locator('#Form_Experience').fill('Kinh nghiệm mới đang chờ duyệt');await tutor.page.getByRole('button',{name:'Gửi hồ sơ xét duyệt',exact:true}).click();
 await learner.page.goto(profileUrl);assert.ok((await learner.page.content()).includes('Kinh nghiệm đã duyệt'));assert.ok(!(await learner.page.content()).includes('Kinh nghiệm mới đang chờ duyệt'));
 await admin.page.goto(origin+'/QuanTri/HoSo');review=admin.page.locator('.review-profile').filter({has:admin.page.getByRole('heading',{name:name,exact:true})});await review.locator('textarea').fill('Cần bổ sung minh chứng kinh nghiệm');await review.getByRole('button',{name:'Từ chối',exact:true}).click();
 await tutor.page.goto(origin+'/HoSoGiaSu/Sua');assert.equal(await tutor.page.locator('#Form_Experience').inputValue(),'Kinh nghiệm mới đang chờ duyệt');
 await learner.page.goto(profileUrl);assert.ok((await learner.page.content()).includes('Kinh nghiệm đã duyệt'));
 pass('conditions update immediately; review/rejection preserves approved content and rejected edits remain prefilled');
 for(const [page,url,label]of [[learner.page,profileUrl,'profile'],[tutor.page,origin+'/HoSoGiaSu/DieuKien','conditions'],[learner.page,origin+'/TinNhan/DanhSachHoiThoai','messages']]){
  await page.setViewportSize({width:360,height:900});await page.goto(url);await noOverflow(page,label);await page.screenshot({path:`${output}/${label}-360.png`,fullPage:true});
 }
 assert.deepEqual(errors,[]);pass('desktop/mobile pages render without server or JavaScript errors');
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
