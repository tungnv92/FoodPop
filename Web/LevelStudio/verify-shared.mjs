import assert from 'node:assert/strict';
import fs from 'node:fs';
import vm from 'node:vm';
const origin='http://127.0.0.1:4173';
vm.runInThisContext(fs.readFileSync('public/studio/lib/js-yaml.min.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/core.js','utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8'));
assert.equal((await fetch(origin+'/api/shared')).status,401);
const signIn=await fetch(origin+'/signin-with-chatgpt?return_to=%2F',{redirect:'manual'}),cookie=signIn.headers.get('set-cookie')?.split(';')[0];assert(cookie,'Local mock sign-in should set a cookie');
// EN: Sends local-only authenticated API requests to exercise shared version writes and reads.
// VI: Gửi yêu cầu API xác thực chỉ ở local để kiểm tra ghi và đọc phiên bản chung.
async function call(query='',body,extra={}){const response=await fetch(origin+'/api/shared'+query,{method:body?'POST':'GET',headers:{Cookie:cookie,...(body?{Origin:origin,'Content-Type':'application/json'}:{}),...extra},body:body?JSON.stringify(body):undefined});const raw=await response.text();let value;try{value=JSON.parse(raw);}catch{value={error:raw};}return {status:response.status,value};}
const me=await call('?action=me');assert.equal(me.value.user.userId,'local_seedy');
const level=Core.parseAsset(catalog.levels[0].yaml.value);level.data.m_Name='API validation '+crypto.randomUUID();
const id=crypto.randomUUID(),body={id,expectedVersion:0,operationId:crypto.randomUUID(),level,note:'Local verification',author:{email:'spoof@example.com'}};
assert.equal((await call('',body,{Origin:'https://untrusted.invalid'})).status,403);
const first=await call('',body);assert.equal(first.status,201);assert.equal(first.value.version,1);assert.equal(first.value.author.email,'seedy@sites.test');
assert.equal((await call('',body)).value.replayed,true);
const attempts=await Promise.all([call('',{...body,expectedVersion:1,operationId:crypto.randomUUID(),note:'Writer A'}),call('',{...body,expectedVersion:1,operationId:crypto.randomUUID(),note:'Writer B'})]);assert.deepEqual(attempts.map(r=>r.status).sort(),[201,409]);
const history=await call('?id='+id+'&action=history');assert.equal(history.value.versions.length,2);assert.deepEqual(history.value.versions.map(v=>v.version),[2,1]);
const sameName=await call('',{...body,id:crypto.randomUUID(),expectedVersion:2,operationId:crypto.randomUUID(),note:'Same name, different browser ID'});assert.equal(sameName.status,201);assert.equal(sameName.value.id,id);assert.equal(sameName.value.version,3);const listed=await call();assert.equal(listed.value.levels.filter(l=>l.name===level.data.m_Name).length,1);assert.equal(listed.value.levels.find(l=>l.id===id).version,3);const found=await call('?action=by-name&name='+encodeURIComponent(level.data.m_Name));assert.equal(found.value.id,id);const old=await call('?id='+id+'&version=1');assert.equal(old.value.note,'Local verification');assert.equal(old.value.level.data.m_Name,level.data.m_Name);
assert.equal((await call('',{...body,id:crypto.randomUUID(),operationId:crypto.randomUUID(),level:{header:'bad',data:{}}})).status,400);
console.log('PASS: unauthenticated rejection, local sign-in, server author identity, cross-origin rejection, immutable history, idempotent retries, concurrent save conflict, old-version retrieval, same-name identity across browser IDs, latest-only listing and payload validation.');
