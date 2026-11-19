const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
// EN: Exercises startup synchronization with isolated API, history and editor state.
// VI: Kiểm tra đồng bộ khi mở trang với API, lịch sử và trạng thái editor độc lập.
async function check(failure=false,backupFailure=false){
 const original=[{header:'h',data:{m_Name:' A ',value:1},_studioId:'a'},{header:'h',data:{m_Name:'A',value:2},_studioId:'duplicate'},{header:'h',data:{m_Name:'LocalOnly',value:3},_studioId:'local'}];
 const state={levels:structuredClone(original),current:0,selected:[2,2],linkCells:[],linkMode:true,crypto:{randomUUID:()=> 'new-id'},C:{clone:structuredClone,exportAsset:l=>JSON.stringify(l),parseAsset:JSON.parse},document:{body:{inert:false},querySelector:()=>({textContent:''})},toast:()=>{},captures:[],change:fn=>fn()};
 state.LevelVersions={capture:async l=>{if(backupFailure)throw Error('Backup failed');state.captures.push(structuredClone(l));}};
 vm.createContext(state);vm.runInContext(fs.readFileSync('public/studio/shared.js','utf8')+'\nglobalThis.shared=Shared;',state);
 let page=0;state.shared.api=async query=>{if(query.startsWith('?action=sync-list'))return {user:{email:'tester'},levels:[{id:++page===1?'a':'b'}],nextAfter:page===1?'cursor':null};if(failure&&query==='?id=b')throw Error('Network failed');return {id:query.slice(4),name:query==='?id=a'?'A':'B',version:4,level:{header:'h',data:{m_Name:query==='?id=a'?'A':'B',value:9}}};};
 await state.shared.syncAll();assert.equal(page,2);assert.equal(state.document.body.inert,false);assert.equal(state.shared.syncing,false);
 if(failure||backupFailure){assert.equal(JSON.stringify(state.levels),JSON.stringify(original));return;}
 assert.equal(state.levels.length,3);assert.equal(state.levels[0].data.value,9);assert.equal(state.levels[0]._studioId,'a');assert.equal(state.levels[0]._sharedVersion,4);assert.equal(state.current,0);assert.equal(state.levels[1].data.m_Name,'LocalOnly');assert.equal(state.levels[2].data.m_Name,'B');assert.equal(state.captures.length,2);
}
// EN: Runs success, network failure and backup failure checks and reports any failure.
// VI: Chạy kiểm tra thành công, lỗi mạng và lỗi sao lưu, báo lỗi nếu có.
(async()=>{await check();await check(true);await check(false,true);console.log('PASS: paginated startup sync, latest server replacement, missing levels added, duplicate names merged, selection/identity preserved, local-only retained, changed copies backed up, network/backup failure leaves workspace intact.');})().catch(error=>{console.error(error);process.exitCode=1;});
