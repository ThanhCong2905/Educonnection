const matchesRoute=require('./route-pattern.cjs');
const {chromium}=require(process.env.EDU_PLAYWRIGHT || 'playwright');
const {execFileSync}=require('child_process');
const assert=require('assert/strict'),fs=require('fs'),crypto=require('crypto');
const origin='http://localhost:5198',suffix=crypto.randomBytes(4).toString('hex');
async function register(browser,role,i) {
 const context=await browser.newContext({viewport:{width:1440,height:1000}}),page=await context.newPage();
 await page.goto(origin+'/Account/Register');await page.locator(`input[value="${role}"]`).check();
 await page.locator('#FullName').fill('Người kiểm thử hồ sơ');const email=`workspace-test-${suffix}-${i}@example.invalid`,password=`Profile testing ${suffix} password`;
 await page.locator('#Email').fill(email);await page.locator('#Phone').fill('090'+String((parseInt(suffix,16)+i)%10000000).padStart(7,'0'));
 await page.locator('#Password').fill(password);await page.locator('#ConfirmPassword').fill(password);await page.locator('#AcceptTerms').check();
 await page.locator('form button[type=submit]').click();await page.waitForURL(url=>matchesRoute(url,'**/Account/Login'));await page.locator('#Identifier').fill(email);await page.locator('#Password').fill(password);
 await page.locator('form button[type=submit]').click();await page.waitForURL(url=>matchesRoute(url,'**/Workspace'));return {context,page};
}
(async()=>{
 execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/Workspace.Ui/database.ps1','-Action','Verify']);
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try {
 const parent=await register(browser,'PhuHuynh',1),tutor=await register(browser,'GiaSu',2);
 fs.mkdirSync('tmp/profile-qa',{recursive:true});
 for(const actor of [parent,tutor]) {
  await actor.page.goto(origin+'/ProfileData/Address');await actor.page.locator('#Form_City').fill('Thành phố kiểm thử');await actor.page.locator('#Form_District').fill('Khu vực thử');await actor.page.locator('#Form_DetailedAddress').fill('SECRET-HOUSE-4321');
  assert.ok(actor.page.url().includes('/Workspace/Profile'));
  await actor.page.locator('form[action*="ProfileData/Address"] button[type=submit]').click();await actor.page.waitForURL(url=>matchesRoute(url,'**/Workspace/Profile#personal-details'));await actor.page.reload();assert.equal(await actor.page.locator('#Form_DetailedAddress').inputValue(),'SECRET-HOUSE-4321');
 }
 console.log('PASS address persistence for parent and tutor');
 await parent.page.goto(origin+'/Workspace/EditNeed');assert.equal(await parent.page.locator('#need-region').inputValue(),'Khu vực thử, Thành phố kiểm thử');await parent.page.locator('#need-format').selectOption('1');assert.equal(await parent.page.locator('#need-address').inputValue(),'SECRET-HOUSE-4321');
 await parent.page.locator('#Form_SubjectId').selectOption({label:'Toán học'});await parent.page.locator('#Form_Grade').selectOption({label:'Lớp 9'});await parent.page.locator('#Form_Goal').fill('Ôn tập kiểm thử');await parent.page.locator('#Form_LearnerName').fill('Học sinh thử');await parent.page.locator('#Form_Fee').fill('100000');await parent.page.locator('[name="Form.Days"][value="1"]').check();
 await parent.page.locator('button[value="true"]').click();await parent.page.waitForURL(url=>matchesRoute(url,'**/Workspace/Needs'));
 await tutor.page.goto(origin+'/Workspace/Search');assert.ok(!(await tutor.page.content()).includes('SECRET-HOUSE-4321'));await tutor.page.locator('.need-card a').first().click();await tutor.page.waitForURL(url=>matchesRoute(url,'**/Workspace/Need/**'));assert.ok(!(await tutor.page.content()).includes('SECRET-HOUSE-4321'));console.log('PASS prefill and no public detailed address');
 await tutor.page.goto(origin+'/Workspace/EditProfile');
 for(const [field,value] of Object.entries({Qualification:'Cử nhân',Expertise:'Toán',Experience:'Dạy học',Introduction:'Giới thiệu',TeachingScope:'THCS',Fee:'200000'}))await tutor.page.locator('#Form_'+field).fill(value);
 await tutor.page.locator('input[name="Form.Subjects"]').first().check();await tutor.page.locator('form[action*="/HoSoGiaSu/"] button[type=submit]').click();await tutor.page.waitForURL(url=>matchesRoute(url,'**/Workspace/Profile'));
 await tutor.page.goto(origin+'/Workspace/EditNeed');assert.equal(await tutor.page.locator('#Form_SubjectId').count(),0);console.log('PASS qualification required even during development bypass');
 await tutor.page.goto(origin+'/ProfileData/Documents');assert.ok(/\/(Workspace\/Profile|HoSoGiaSu\/ChiTiet)/.test(tutor.page.url()));await tutor.page.locator('#proof-file').setInputFiles({name:'bang-cap.pdf',mimeType:'application/pdf',buffer:Buffer.from('%PDF-1.4\nTest document')});await tutor.page.locator('form[enctype] button').click();await tutor.page.waitForURL(url=>matchesRoute(url,'**/Workspace/Profile#qualifications-files'));
 const download=tutor.page.locator('a[href*="Download"]');assert.equal(await download.count(),1);const href=await download.getAttribute('href');assert.equal((await tutor.page.request.get(origin+href)).status(),200);assert.equal((await parent.page.request.get(origin+href)).status(),403);
 await tutor.page.screenshot({path:'tmp/profile-qa/documents-desktop.png',fullPage:true});await tutor.page.setViewportSize({width:360,height:844});await tutor.page.screenshot({path:'tmp/profile-qa/documents-mobile.png',fullPage:true});assert.ok(await tutor.page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
 await tutor.page.locator('form[action*="Delete"] button').click();await tutor.page.waitForURL(url=>matchesRoute(url,'**/Workspace/Profile#qualifications-files'));assert.equal(await tutor.page.locator('a[href*="Download"]').count(),0);console.log('PASS upload, download permissions, delete and mobile layout');
 await parent.page.goto(origin+'/ProfileData/Address');await parent.page.screenshot({path:'tmp/profile-qa/address-desktop.png',fullPage:true});await parent.page.setViewportSize({width:360,height:844});await parent.page.screenshot({path:'tmp/profile-qa/address-mobile.png',fullPage:true});assert.ok(await parent.page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
