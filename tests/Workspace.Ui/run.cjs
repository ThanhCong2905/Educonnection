const {chromium}=require('playwright');
const {execFileSync}=require('child_process');
const fs=require('fs'),crypto=require('crypto'),assert=require('assert/strict');
const suffix=crypto.randomBytes(4).toString('hex'),password=`Workspace ${suffix} test password`;
const origin='http://localhost:5198';
const results=[],errors=[];
function pass(name){results.push(name);console.log('PASS '+name)}
async function register(browser,role,index){
 const context=await browser.newContext({viewport:{width:1440,height:1000}});const page=await context.newPage();
 page.on('pageerror',error=>errors.push(error.message));
 await page.goto(origin+'/Account/Register');await page.locator(`input[value="${role}"]`).check();
 await page.locator('#FullName').fill(role==='GiaSu'?'Gia sư kiểm thử':role==='PhuHuynh'?'Phụ huynh kiểm thử':'Học sinh kiểm thử');
 const email=`workspace-test-${suffix}-${index}@example.invalid`;
 await page.locator('#Email').fill(email);await page.locator('#Phone').fill('090'+String((parseInt(suffix,16)+index)%10000000).padStart(7,'0'));
 await page.locator('#Password').fill(password);await page.locator('#ConfirmPassword').fill(password);await page.locator('#AcceptTerms').check();
 await page.getByRole('button',{name:'Tạo tài khoản',exact:true}).click();await page.waitForURL('**/Account/Login');
 await page.locator('#Identifier').fill(email);await page.locator('#Password').fill(password);await page.getByRole('button',{name:'Đăng nhập',exact:true}).click();
 await page.waitForURL('**/Workspace');
 await page.goto(origin+'/Workspace/Profile');await page.locator('#Form_City').fill('Thành phố kiểm thử');await page.locator('#Form_DetailedAddress').fill('Địa chỉ riêng tư kiểm thử');await page.locator('form[action*="ProfileData/Address"] button').click();await page.waitForURL('**/Workspace/Profile#personal-details');await page.goto(origin+'/Workspace');return {context,page,email};
}
async function screenshot(page,name){await page.evaluate(()=>document.fonts.ready);await page.screenshot({path:'tmp/workspace-screenshots/'+name+'.png',fullPage:true,animations:'disabled'});}
async function createNeed(page,grade,fee,parent=false){
 await page.goto(origin+'/Workspace/EditNeed');await page.locator('#Form_SubjectId').selectOption({label:'Toán học'});
 await page.locator('#Form_Grade').selectOption({label:grade});await page.locator('#Form_Goal').fill('Củng cố kiến thức, ôn tập và tiến bộ trong môn Toán.');
 await page.locator('#Form_Fee').fill(String(fee));if(parent)await page.locator('#Form_LearnerName').fill('Tên riêng tư của người học');
 await page.locator('input[name="Form.Days"][value="1"]').check();await page.locator('input[name="Form.Days"][value="3"]').check();
 await page.getByRole('button',{name:'Mở nhu cầu',exact:true}).click();await page.waitForURL('**/Workspace/Needs');
}
(async()=>{
 execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/Workspace.Ui/database.ps1','-Action','Verify']);
 fs.mkdirSync('tmp/workspace-screenshots',{recursive:true});
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 const student=await register(browser,'HocSinh',1);await screenshot(student.page,'student-home-empty');
 assert.ok(await student.page.getByRole('link',{name:'Nhu cầu học',exact:true}).count());pass('student home and navigation');
 const parent=await register(browser,'PhuHuynh',2);await parent.page.goto(origin+'/Workspace/EditNeed');
 assert.equal(await parent.page.locator('#Form_LearnerName').count(),1);await screenshot(parent.page,'parent-need-form');pass('parent form includes represented learner');
 const tutor=await register(browser,'GiaSu',3);await screenshot(tutor.page,'tutor-home-pending');
 await tutor.page.goto(origin+'/Workspace/EditNeed');assert.ok(await tutor.page.getByRole('heading',{name:'Vui lòng hoàn thiện hồ sơ cá nhân'}).count());pass('incomplete tutor sees profile gate');
 await tutor.page.goto(origin+'/Workspace/EditProfile');
 await tutor.page.locator('#Form_Qualification').fill('Cử nhân Sư phạm Toán');await tutor.page.locator('#Form_Expertise').fill('Toán THCS và THPT');
 await tutor.page.locator('#Form_Experience').fill('Hỗ trợ học sinh củng cố kiến thức và ôn thi.');await tutor.page.locator('#Form_Introduction').fill('Đồng hành cùng học sinh với kế hoạch học tập rõ ràng, bài tập phù hợp và phản hồi sau mỗi buổi.');
 await tutor.page.locator('input[name="Form.Subjects"]').first().check();await tutor.page.locator('#Form_TeachingScope').fill('Toán lớp 6 đến lớp 12');
 await tutor.page.locator('#Form_Region').fill('Hà Nội');await tutor.page.locator('#Form_Fee').fill('250000');
 await tutor.page.getByRole('button',{name:'Lưu thông tin chuyên môn',exact:true}).click();await tutor.page.waitForURL('**/Workspace/Profile');
 await tutor.page.locator('#proof-file').setInputFiles({name:'bang-cap.pdf',mimeType:'application/pdf',buffer:Buffer.from('%PDF-1.4\nTest')});await tutor.page.locator('form[enctype] button').click();await tutor.page.waitForURL('**/Workspace/Profile#qualifications-files');
 assert.ok(await tutor.page.getByText('Đang chờ xét duyệt',{exact:true}).count());await screenshot(tutor.page,'tutor-profile-pending');pass('tutor creates pending profile');
 execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/Workspace.Ui/database.ps1','-Action','Approve','-Email',tutor.email]);
 for(let i=0;i<4;i++)await createNeed(tutor.page,'Lớp '+(9+i),200000+i*50000);
 await screenshot(tutor.page,'tutor-needs-populated');pass('approved tutor creates and lists teaching needs');
 await createNeed(parent.page,'Lớp 9',250000,true);await screenshot(parent.page,'parent-needs-populated');pass('parent publishes learning need');
 await parent.page.goto(origin+'/Workspace');await screenshot(parent.page,'parent-home-with-needs');
 assert.ok(await parent.page.getByRole('heading',{name:'Chưa có kết quả xét độ phù hợp'}).count());pass('home distinguishes existing needs from calculated compatibility');
 await student.page.goto(origin+'/Workspace/Search');assert.equal(await student.page.locator('.need-card').count(),4);await screenshot(student.page,'student-search-populated');
 await student.page.locator('#Grade').selectOption({label:'Lớp 9'});await student.page.locator('.search-line button').click();await student.page.waitForLoadState('networkidle');assert.equal(await student.page.locator('.need-card').count(),1);pass('grade filter returns matching need only');
 await student.page.goto(origin+'/Workspace/Search');await student.page.locator('#MaxFee').fill('210000');await student.page.locator('#FeeUnit').selectOption('1');await student.page.locator('.search-line button').click();await student.page.waitForLoadState('networkidle');assert.equal(await student.page.locator('.need-card').count(),1);pass('fee filter uses selected unit');
 await student.page.goto(origin+'/Workspace/Search');const checkboxes=student.page.locator('[data-compare]');for(let i=0;i<3;i++)await checkboxes.nth(i).check();
 await checkboxes.nth(3).click();assert.equal(await checkboxes.nth(3).isChecked(),false);await student.page.locator('#open-comparison').click();await student.page.locator('#comparison-modal').waitFor({state:'visible'});
 assert.equal(await student.page.locator('.comparison-table tr').count(),7);await screenshot(student.page,'comparison');await student.page.locator('#comparison-modal .btn-close').click();pass('comparison supports up to three needs');
 const href=await student.page.locator('.need-card a').first().getAttribute('href');await student.page.goto(origin+href);assert.ok(await student.page.getByRole('heading',{name:'Giới thiệu bản thân'}).count());assert.equal(await student.page.locator('[data-compare]').count(),0);assert.equal(await student.page.locator('#compare-tray').isVisible(),false);await screenshot(student.page,'student-tutor-profile');
 await student.page.getByRole('tab',{name:'Chuyên môn & trình độ'}).click();assert.equal(await student.page.locator('#qualifications').isVisible(),true);await screenshot(student.page,'student-tutor-qualification');pass('profile tabs and source fields');
 await tutor.page.goto(origin+'/Workspace/Search');assert.equal(await tutor.page.locator('.need-card').count(),1);assert.ok(!(await tutor.page.content()).includes('Tên riêng tư của người học'));await screenshot(tutor.page,'tutor-search-learning-needs');
 await tutor.page.locator('.need-card a').first().click();await tutor.page.waitForURL('**/Workspace/Need/**');await screenshot(tutor.page,'tutor-learning-need-detail');pass('tutor sees public learning need without learner identity');
 const protectedResponse=await student.page.request.get(origin+'/Workspace/EditProfile');assert.equal(protectedResponse.status(),403);pass('student cannot access tutor profile editor');
 for(const [actor,name] of [[student,'student'],[parent,'parent'],[tutor,'tutor']]){
  for(const width of [360,768]){await actor.page.setViewportSize({width,height:844});for(const [path,label] of [['/Workspace','home'],['/Workspace/Search','search'],['/Workspace/Needs','needs'],['/Workspace/Profile','profile']]){
   await actor.page.goto(origin+path);assert.ok(await actor.page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),name+' '+label+' no overflow '+width);await screenshot(actor.page,`${name}-${label}-${width}`);
  }}
 }
 pass('all roles and four page groups fit mobile/tablet');
 assert.deepEqual(errors,[]);pass('no browser JavaScript errors');
 fs.writeFileSync('tmp/workspace-ui-results.json',JSON.stringify({checks:results,passed:results.length,errors},null,2));await browser.close();console.log('COMPLETED '+results.length+' checks');
})().catch(error=>{console.error(error);process.exitCode=1;});

