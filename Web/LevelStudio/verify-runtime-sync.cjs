const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict');
for(const file of ['lib/js-yaml.min.js','core.js','play-engine.js'])vm.runInThisContext(fs.readFileSync('public/studio/'+file,'utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8'));Core.setCatalog(catalog);
// EN: Creates one explicit recipe and a single board dish for independent runtime boundary checks.
// VI: Tạo một công thức trực tiếp và một đĩa board để kiểm tra độc lập các biên runtime.
function create(types=[1,2],foodType=1,count=2,type=0,time=30){return new LevelSimulation({boardColumns:1,boardRows:1,ingredientBoard:[{foods:[{...Core.emptyCell(),foodType,requiredCount:count}]}],dishLinks:[],useUnifiedCustomerQueue:true,customerQueue:[{customerType:type,timeServe:time,useIngredientOrders:true,ingredientOrders:[{ingredients:types}]}]},catalog);}
// EN: Fills occupied slots with prepared, unneeded portions to isolate capacity decisions from chef activity.
// VI: Điền slot đang dùng bằng phần đã chế biến không cần để tách quyết định sức chứa khỏi hoạt động đầu bếp.
function occupy(sim,free){for(let i=free;i<10;i++)sim.slots[i].item={type:27,state:'prepared',landedAt:0};}
let checks=0;
for(let free=0;free<=10;free++)for(let n=1;n<=12;n++)for(const busy of [false,true]){
 const sim=create([1,2],1,n);occupy(sim,free);if(busy)sim.chefItem={state:'preparing',readyAt:100};
 const allowed=n<=free||n===free+1&&!busy;assert.equal(!sim.rejection(sim.dishes[0]),allowed);checks++;
}
let sim=create();occupy(sim,1);assert(sim.pick(0));assert(sim.chefItem.direct);assert.equal(sim.boardCapacity(),0);assert.equal(sim.target(1),null);sim.update(4);assert.equal(sim.customers[0].dishes[0].arrived.filter(x=>x===1).length,1);
sim=create([1,2],2,2);occupy(sim,1);assert(!sim.pick(0),'Non-base overflow must be rejected');assert.equal(sim.customers[0].dishes[0].missing.length,2);
sim=create([1,2],2,2);sim.slots[9].item={type:1,state:'raw'};assert(sim.pick(0));assert.equal(sim.chefItem,null,'Landed higher-priority belt food beats board food');sim.update(.025);assert.equal(sim.chefItem.type,1);assert.equal(sim.chefItem.destination,9,'Chef can select outside station');
sim=create([1,2],1,1);sim.slots[0].item={type:1,state:'prepared',landedAt:0};sim.phase=.5;sim.update(.25);assert.equal(sim.customers[0].dishes[0].incoming,null);sim.update(.1);assert(sim.customers[0].dishes[0].incoming,'Serve outside exit after landing delay');
sim=create([1,2],1,1,1,.5);sim.update(.6);assert.equal(sim.customers[0].timeLeft,0);assert.equal(sim.failed,null,'Current Unity controller does not fail on timer expiry');const frozen=sim.time;sim.update(0);assert.equal(sim.time,frozen);

sim=create([1],1,1,0,.1);sim.update(1);assert.equal(sim.failed,null,'Normal customer has no timer');
sim=create([1,1,2],1,1);occupy(sim,0);assert(sim.pick(0));assert.equal(sim.customers[0].dishes[0].missing.filter(x=>x===1).length,1);assert(sim.chefItem.direct);sim.update(4);assert.equal(sim.customers[0].dishes[0].arrived.length,1);
assert.equal(create().actionDuration(18),2);assert.equal(create().actionDuration(1),1.8333);
const spine=JSON.parse(fs.readFileSync('public/studio/spine/chef-v3/Chef.json'));for(const action of Object.values(catalog.gameplay.actions))assert(spine.animations[action.name]);
console.log('PASS:',checks,'capacity boundaries; direct reservation, base-first overflow, raw priority anywhere, landing delay, local timer/pause, repeated base, animation mapping.');
