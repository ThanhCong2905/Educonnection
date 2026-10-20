const {request,form,setNetwork}=require('./auth-flow-test.cjs');
const fs=require('fs'),assert=require('assert/strict'),{execFileSync}=require('child_process');
const {email,suffix,network}=JSON.parse(fs.readFileSync('tmp/auth-test-identity.json','utf8'));setNetwork(network);
function sql(action){execFileSync('powershell',['-NoProfile','-ExecutionPolicy','Bypass','-File','tests/auth/auth-db-test.ps1','-Action',action,'-Email',email]);}
(async()=>{
 const jar=new Map();let r=await form('/Account/Login',{Identifier:email,Password:`Reset ${suffix} secret passphrase`},'127.0.0.84',jar);assert.equal(r.status,302);
 const original=new Map(jar);sql('Lock');r=await request('/Account/Overview',undefined,'127.0.0.84',jar);assert.equal(r.status,302);
 sql('Unlock');r=await request('/Account/Overview',undefined,'127.0.0.84',original);assert.equal(r.status,302);
 console.log('PASS old cookie stays revoked after unlock');
})().catch(e=>{try{sql('Unlock')}catch{}console.error(e);process.exitCode=1;});

