const {request,form,setNetwork}=require('./auth-flow-test.cjs');
const {execFileSync}=require('child_process');
const fs=require('fs');const assert=require('assert/strict');
const {email,suffix,network}=JSON.parse(fs.readFileSync('tmp/auth-test-identity.json','utf8'));
setNetwork(network);
const password=`Reset ${suffix} secret passphrase`;
const results=[];const pass=x=>{results.push(x);console.log('PASS '+x)};
function sql(action,target=email){return execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/auth/auth-db-test.ps1','-Action',action,'-Email',target],{encoding:'utf8'}).trim();}
(async()=>{
 let r=await request('/Account/ChangePassword');assert.equal(r.status,302);pass('change password requires authentication');
 const jar=new Map();
 r=await form('/Account/Login',{Identifier:email,Password:password},'127.0.0.83',jar);
 if(r.status===429 && Number(r.headers['retry-after'])<=30){await new Promise(resolve=>setTimeout(resolve,(Number(r.headers['retry-after'])+1)*1000));r=await form('/Account/Login',{Identifier:email,Password:password},'127.0.0.83',jar);}
 assert.equal(r.status,302);
 sql('Expire');r=await request('/Account/Overview',undefined,'127.0.0.83',jar);assert.equal(r.status,302);pass('30-minute inactivity expires session');
 r=await form('/Account/Login',{Identifier:email,Password:password},'127.0.0.83',jar);assert.equal(r.status,302);
 const originalCookie=new Map(jar);
 sql('Lock');r=await request('/Account/Overview',undefined,'127.0.0.83',jar);assert.equal(r.status,302);pass('locked account loses existing session');
 r=await form('/Account/Login',{Identifier:email,Password:password},'127.0.0.83',jar);assert.equal(r.status,200);assert.ok(r.body.includes('validation-summary-errors'));pass('locked account cannot log in');sql('Unlock');
 r=await request('/Account/Overview',undefined,'127.0.0.83',originalCookie);assert.equal(r.status,302);pass('old cookie stays revoked after account is unlocked');
 for(const [i,role] of ['PhuHuynh','GiaSu'].entries()){
  const roleEmail=`auth-test-${suffix}-${role.toLowerCase()}@example.invalid`;
  const fields={Role:role,FullName:'Kiá»ƒm thá»­ '+role,Email:roleEmail,Phone:'098'+String((parseInt(suffix,16)+i)%10000000).padStart(7,'0'),Password:password,ConfirmPassword:password,AcceptTerms:'true'};
  r=await form('/Account/Register',fields,'127.0.0.81',new Map());assert.equal(r.status,302);assert.equal(sql('Roles',roleEmail),role);pass('register and persist role '+role);
  r=await form('/Account/Register',{...fields,Email:`auth-test-${suffix}-duplicate@example.invalid`},'127.0.0.81',new Map());assert.equal(r.status,200);assert.ok(r.body.includes('validation-summary-errors'));pass('duplicate phone rejected '+role);
 }
 const noTerms={Role:'HocSinh',FullName:'Kiá»ƒm thá»­ Ä‘iá»u khoáº£n',Email:`auth-test-${suffix}-terms@example.invalid`,Phone:'0901112233',Password:password,ConfirmPassword:password,AcceptTerms:'false'};
 r=await form('/Account/Register',noTerms,'127.0.0.81',new Map());assert.equal(r.status,200);assert.ok(r.body.includes('field-validation-error'));pass('terms checkbox validated on server');
 const invalid={...noTerms,AcceptTerms:'true',ConfirmPassword:'A different password'};
 r=await form('/Account/Register',invalid,'127.0.0.81',new Map());assert.equal(r.status,200);assert.ok(r.body.includes('field-validation-error'));pass('confirmation mismatch validated on server');
 const unknown=await form('/Account/ForgotPassword',{Email:`auth-test-${suffix}-unknown@example.invalid`},'127.0.0.82',new Map());assert.equal(unknown.status,302);assert.equal(unknown.headers.location,'/Account/ForgotPasswordConfirmation');pass('forgot password does not reveal unknown email');
 const jar2=new Map();r=await form('/Account/Login',{Identifier:email,Password:password},'127.0.0.83',jar2);assert.equal(r.status,302);
 const response=await new Promise((resolve,reject)=>{const http=require('http');const req=http.get({hostname:'127.0.0.1',port:5198,path:'/Account/Overview',headers:{Accept:'application/json'}},res=>{res.resume();res.on('end',()=>resolve(res.statusCode));});req.on('error',reject);});assert.equal(response,401);pass('unauthenticated JSON request gets 401');
 fs.writeFileSync('tmp/auth-edge-results.json',JSON.stringify({checks:results,passed:results.length},null,2));
 console.log(`COMPLETED ${results.length} edge checks`);
})().catch(e=>{try{sql('Unlock')}catch{}console.error(e);process.exitCode=1;});

