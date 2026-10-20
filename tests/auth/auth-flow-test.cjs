const http = require('http');
const fs = require('fs');
const crypto = require('crypto');
const assert = require('assert/strict');
const suffix = crypto.randomBytes(5).toString('hex');
let network = `127.${parseInt(suffix.slice(0,2),16)}.${parseInt(suffix.slice(2,4),16)}.`;
const email = `auth-test-${suffix}@example.invalid`;
const password = `Initial ${suffix} secret passphrase`;
const updated = `Changed ${suffix} secret passphrase`;
const resetPassword = `Reset ${suffix} secret passphrase`;
const cookies = new Map();
const results = [];
function request(path, fields, address = '127.0.0.77', cookieJar = cookies) {
  address = address.replace('127.0.0.',network);
  return new Promise((resolve,reject) => {
    const data = fields ? new URLSearchParams(fields).toString() : '';
    const req = http.request({hostname:'127.0.0.1',port:5198,path,localAddress:address,method:fields?'POST':'GET',headers:{Cookie:[...cookieJar].map(([k,v])=>`${k}=${v}`).join('; '),...(fields?{'Content-Type':'application/x-www-form-urlencoded','Content-Length':Buffer.byteLength(data)}:{})}},res=>{
      for(const item of res.headers['set-cookie']||[]) { const pair = item.split(';')[0]; const at=pair.indexOf('='); cookieJar.set(pair.slice(0,at),pair.slice(at+1)); }
      let body=''; res.setEncoding('utf8');res.on('data',x=>body+=x);res.on('end',()=>resolve({status:res.statusCode,body,headers:res.headers}));
    });req.on('error',reject);req.end(data);
  });
}
async function form(path, fields, address, jar) {
  const get = await request(path === '/Account/Logout' ? '/Account/Overview' : path,undefined,address,jar);
  assert.equal(get.status,200,`GET ${path}`);
  const token = /name="__RequestVerificationToken"[^>]*value="([^"]+)"/.exec(get.body)?.[1];
  assert.ok(token,`antiforgery ${path}`);
  return request(path,{...fields,__RequestVerificationToken:token},address,jar);
}
function pass(name){results.push(name);console.log('PASS '+name);}
async function run(){
 fs.writeFileSync('tmp/auth-test-identity.json',JSON.stringify({email,suffix,network}));
 for(const path of ['/','/Account/Login','/Account/Register','/Account/ForgotPassword','/Account/ForgotPasswordConfirmation','/Account/ResetPassword','/Account/AccessDenied','/Home/Information?section=privacy','/css/auth.css','/css/fonts.css','/images/auth/logo.png','/fonts/be-vietnam-pro-vietnamese-400-normal.woff2']){
   const r=await request(path);assert.equal(r.status,path.endsWith('AccessDenied')?403:200,path);
 }pass('all public pages, styles, font and logo respond');
 assert.equal((await request('/Account/Overview')).status,302);pass('protected page redirects unauthenticated user');
 assert.equal((await request('/Account/Login',{Identifier:email,Password:password})).status,400);pass('POST without antiforgery rejected');
 const base={Role:'HocSinh',FullName:'Tài khoản kiểm thử',Email:email,Phone:'097'+String(parseInt(suffix,16)%10000000).padStart(7,'0'),Password:password,ConfirmPassword:password,AcceptTerms:'true'};
 let r=await form('/Account/Register',{...base,Role:'Admin'});assert.equal(r.status,200);assert.ok(r.body.includes('field-validation-error'));pass('cannot self-register as admin');
 r=await form('/Account/Register',{...base,Password:'short',ConfirmPassword:'short'});assert.equal(r.status,200);assert.ok(r.body.includes('field-validation-error'));pass('short password rejected on server');
 r=await form('/Account/Register',{...base,Password:'passwordpassword',ConfirmPassword:'passwordpassword'});assert.equal(r.status,200);assert.ok(r.body.includes('validation-summary-errors'));pass('common password rejected');
 r=await form('/Account/Register',base);assert.equal(r.status,302);assert.equal(r.headers.location,'/Account/Login');pass('registration creates account and role');
 r=await form('/Account/Register',base);assert.equal(r.status,200);assert.ok(r.body.includes('validation-summary-errors'));pass('duplicate registration rejected');
 r=await form('/Account/Login',{Identifier:email,Password:'wrong secret password'});assert.equal(r.status,200);assert.ok(r.body.includes('validation-summary-errors'));pass('incorrect credentials show error');
 r=await form('/Account/Login',{Identifier:base.Phone,Password:password,ReturnUrl:'https://evil.invalid'});assert.equal(r.status,302);assert.equal(r.headers.location,'/Workspace');pass('phone login works and external return URL ignored');
 r=await request('/Account/Overview');assert.equal(r.status,200);assert.ok(r.body.includes(email));pass('authenticated overview shows account');
 r=await form('/Account/ChangePassword',{CurrentPassword:password,Password:updated,ConfirmPassword:updated},'127.0.0.78');assert.equal(r.status,302);pass('change password revokes session');
 assert.equal((await request('/Account/Overview')).status,302);
 r=await form('/Account/Login',{Identifier:email,Password:updated},'127.0.0.78');assert.equal(r.status,302);pass('login with changed password works');
 r=await form('/Account/Logout',{},'127.0.0.78');assert.equal(r.status,302);assert.equal((await request('/Account/Overview')).status,302);pass('logout removes access');
 const before = new Set(fs.existsSync('.maildrop')?fs.readdirSync('.maildrop'):[]);
 r=await form('/Account/ForgotPassword',{Email:email},'127.0.0.78');assert.equal(r.status,302);assert.equal(r.headers.location,'/Account/ForgotPasswordConfirmation');
 const mailFile=fs.readdirSync('.maildrop').find(x=>!before.has(x));assert.ok(mailFile,'reset mail created');
 const mail=fs.readFileSync('.maildrop/'+mailFile,'utf8').replace(/=\r?\n/g,'').replace(/=([0-9A-F]{2})/g,(_,h)=>String.fromCharCode(parseInt(h,16)));
 let link=mail.match(/http:\/\/localhost:5187\/Account\/ResetPassword[^\s]+/)?.[0];
 if(!link){const body=mail.split(/\r?\n\r?\n/).slice(1).join('\n');const decoded=Buffer.from(body.replace(/\s/g,''),'base64').toString('utf8');link=decoded.match(/http:\/\/localhost:5187\/Account\/ResetPassword[^\s]+/)?.[0];}
 assert.ok(link,'mail contains reset URL');const url=new URL(link);const code=url.searchParams.get('code');assert.ok(code);pass('forgot password writes reset email outside wwwroot');
 r=await form('/Account/ResetPassword?email='+encodeURIComponent(email)+'&code='+encodeURIComponent(code),{Email:email,Code:code,Password:resetPassword,ConfirmPassword:resetPassword},'127.0.0.78');assert.equal(r.status,302);pass('reset token changes password');
 r=await form('/Account/ResetPassword?email='+encodeURIComponent(email)+'&code='+encodeURIComponent(code),{Email:email,Code:code,Password:password,ConfirmPassword:password},'127.0.0.78');assert.equal(r.status,200);assert.ok(!r.body.includes('id="Code"'));pass('reset token cannot be reused');
 r=await form('/Account/Login',{Identifier:email,Password:resetPassword},'127.0.0.78');assert.equal(r.status,302);pass('login with reset password works');
 // Independent test IP exercises account/IP backoff without affecting normal local sessions.
 const jar=new Map(); for(let i=0;i<5;i++){r=await form('/Account/Login',{Identifier:email,Password:'incorrect long password'},'127.0.0.79',jar);assert.equal(r.status,i===4?429:200);}
 assert.ok(Number(r.headers['retry-after'])>0);pass('fifth failed login receives 30-second delay');
 r=await form('/Account/Login',{Identifier:base.Phone,Password:resetPassword},'127.0.0.80',new Map());assert.equal(r.status,429);pass('phone alias and another IP cannot bypass account delay');
 fs.writeFileSync('tmp/auth-flow-results.json',JSON.stringify({passed:results.length,checks:results},null,2));
 console.log(`COMPLETED ${results.length} checks`);
}
module.exports={request,form,setNetwork:value=>{network=value;}};
if(require.main===module)run().catch(e=>{console.error(e);process.exitCode=1;});
