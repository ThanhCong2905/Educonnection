const {chromium}=require(process.env.EDU_PLAYWRIGHT||'playwright');
const assert=require('assert/strict');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'});
 try{
  for(const width of [1440,360]){
   const page=await browser.newPage({viewport:{width,height:900}});
   await page.setContent('<main style="margin:40px"><h1>Trang đang sử dụng</h1><button id="action">Tiếp tục</button></main><aside class="flash-region"><div class="flash-message flash-success" data-flash role="status"><span>Đã lưu thông tin thành công.</span><button data-flash-close aria-label="Đóng thông báo">×</button></div><div class="flash-message flash-error" data-flash role="alert"><span>Vui lòng kiểm tra thông tin.</span><button data-flash-close aria-label="Đóng thông báo">×</button></div></aside>');
   await page.addStyleTag({path:'wwwroot/css/notifications.css'});
   const before=await page.locator('main').boundingBox(),started=Date.now();
   await page.addScriptTag({path:'wwwroot/js/notifications.js'});
   assert.equal(await page.locator('[data-flash]').count(),2);
   assert.equal(await page.locator('.flash-region').evaluate(e=>getComputedStyle(e).position),'fixed');
   assert.equal(await page.locator('.flash-region').evaluate(e=>getComputedStyle(e).pointerEvents),'none');
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
   await page.locator('#action').click();
   await page.locator('[data-flash-close]').first().click();assert.equal(await page.locator('[data-flash]').count(),1);
   await page.waitForFunction(()=>document.querySelectorAll('[data-flash]').length===0,{},{timeout:7000});
   assert.ok(Date.now()-started>=4800);assert.equal(await page.locator('.flash-region').count(),0);
   assert.deepEqual(await page.locator('main').boundingBox(),before);
   console.log(`PASS ${width}px: compact corner notices, close button, 5-second dismissal, no layout shift`);
   await page.close();
  }
 }finally{await browser.close();}
})().catch(error=>{console.error(error);process.exitCode=1;});
