const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
vm.runInThisContext(fs.readFileSync('public/studio/lib/js-yaml.min.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/core.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/play-engine.js','utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8'));
// EN: Returns a fresh bundled level so each gameplay check has isolated state.
// VI: Lấy level mẫu mới để mỗi kiểm tra gameplay có trạng thái độc lập.
function level(){return Core.parseAsset(catalog.levels[0].yaml.value).data;}
let data=level(),s=new LevelSimulation(data,catalog),original=JSON.stringify(data);assert(!s.pick(s.dishes.find(d=>d.y===1).id));const first=s.dishes.find(d=>d.x===0&&d.y===0);const below=s.dishes.find(d=>d.x===0&&d.y===1);assert(s.pick(first.id));assert.equal(below.y,0);assert.equal(s.moves,1);assert.equal(JSON.stringify(data),original);assert.equal(s.slots.filter(a=>a.item).length,1);assert.equal(s.chefItem.state,'lifting');s.update(.5);assert(s.chefItem?.state==='preparing','Nearest entrance slot should reach preparation immediately after landing');
data=level();data.ingredientBoard[0].foods[0].requiredCount=12;s=new LevelSimulation(data,catalog);const snapshot=JSON.stringify(s.dishes);assert(!s.pick(s.dishes[0].id));assert.equal(JSON.stringify(s.dishes),snapshot);assert.equal(s.slots.filter(a=>a.item).length,0);
data=level();data.ingredientBoard[1].foods[0].isFrozen=1;data.ingredientBoard[1].foods[0].blockValue=2;data.dishLinks=[{dishes:[{x:0,y:0},{x:2,y:0}]}];s=new LevelSimulation(data,catalog);assert(s.pick(s.dishes.find(d=>d.x===0&&d.y===0).id));assert.equal(s.moves,1);assert.equal(s.dishes.find(d=>d.x===1&&d.y===0).blockValue,1);assert.equal(s.slots.filter(a=>a.item).length,2);
data=level();data.ingredientBoard[0].foods[0].dishShape=2;data.ingredientBoard[0].foods[0].secondFoodType=2;data.ingredientBoard[0].foods[0].secondCount=2;data.ingredientBoard[1].foods[0]=Core.emptyCell();s=new LevelSimulation(data,catalog);assert(s.pick(s.dishes.find(d=>d.x===0&&d.y===0).id));assert.equal(s.slots.filter(a=>a.item).length,3);assert.equal(s.dishes.find(d=>d.x===0).y,0);assert.equal(s.dishes.find(d=>d.x===1).y,0);
data=level();data.dishLinks=[{dishes:[{x:0,y:0},{x:0,y:1}]}];data.ingredientBoard[0].foods[1].isHidden=1;s=new LevelSimulation(data,catalog);assert(!s.dishes.find(d=>d.x===0&&d.y===1).isHidden,'Unity opens initially exposed lids without animation');s.update(.36);assert(!s.dishes.find(d=>d.x===0&&d.y===1).isHidden);
s=new LevelSimulation(level(),catalog);assert.equal(s.target(2),null);assert(s.target(1));
// EN: Plays eligible head dishes, checking level completion and conservation with reserved chef plates.
// VI: Chơi đĩa đầu hợp lệ, kiểm tra hoàn thành và bảo toàn nguyên liệu khi đĩa sơ chế giữ chỗ.
function autoplay(sim){for(let t=0;t<6000&&!sim.won;t++){if(t%5===0){const dish=sim.dishes.find(d=>!sim.rejection(d));if(dish)sim.pick(dish.id);}sim.update(.1);}return sim;}
s=autoplay(new LevelSimulation(level(),catalog));assert(s.won,'Sample should complete automatically');assert.equal(s.served,4);assert.equal(s.dishes.length,0);assert(s.slots.every(a=>!a.item));assert.equal(s.lanes.flatMap(l=>l.customers).flatMap(c=>c.dishes).reduce((n,d)=>n+d.arrived.length,0),15);
console.log('PASS: head-only selection, whole-dish capacity, upward settling, atomic links, ice once per action, double dishes, hidden reveal, base-first service, automatic level completion, conservation and editor isolation.');

const sharedData=level();sharedData.customerQueue=[{finalFoods:[13]},{finalFoods:[12]},{finalFoods:[12]},{finalFoods:[13]}];const seats=new LevelSimulation(sharedData,catalog);assert.equal(seats.customer(seats.lanes[0]).number,1);assert.equal(seats.customer(seats.lanes[1]).number,2);seats.customer(seats.lanes[1]).dishes.forEach(d=>d.done=true);seats.update(.025);assert.equal(seats.customer(seats.lanes[1]),null);seats.update(1.8);assert.equal(seats.customer(seats.lanes[1]).number,3);assert.equal(seats.customer(seats.lanes[0]).number,1);seats.customer(seats.lanes[0]).dishes.forEach(d=>d.done=true);seats.update(1.8);assert.equal(seats.customer(seats.lanes[0]).number,4);assert.equal(Core.analyze(sharedData,catalog).customers,4);const compatible=Core.parseAsset(Core.exportAsset({header:Core.parseAsset(catalog.levels[0].yaml.value).header,data:sharedData}));assert.deepEqual(Core.customerQueue(compatible.data).map(c=>c.finalFoods),sharedData.customerQueue.map(c=>c.finalFoods));console.log('PASS: one queue feeds two seats in global order even when the right customer finishes first; legacy export retains order and totals.');

// EN: Checks ten evenly spaced belt slots and atomic rejection when the belt is full.
// VI: Kiểm tra mười ô băng chuyền cách đều và từ chối toàn bộ lượt lấy khi băng đã đầy.
const tenData=level();tenData.ingredientBoard[0].foods[0].requiredCount=10;
const tenBelt=new LevelSimulation(tenData,catalog);
assert.equal(tenBelt.slots.length,10);
for(let i=0;i<9;i++)assert(Math.abs(tenBelt.position(tenBelt.slots[i+1])-tenBelt.position(tenBelt.slots[i])-.1)<1e-9);
assert(tenBelt.pick(tenBelt.dishes.find(d=>d.x===0&&d.y===0).id));
assert.equal(tenBelt.slots.filter(s=>s.item).length,10);
const extra=tenBelt.dishes.find(d=>tenBelt.head(d));
assert(!tenBelt.pick(extra.id));
assert.equal(tenBelt.slots.filter(s=>s.item).length,10);
console.log('PASS: exactly 10 evenly spaced slots, ten-item pickup accepted, further pickup rejected while chef is busy; oversized pickup rejected.');

// EN: Verifies entrance-distance priority across wraparound and ignores occupied slots without partial reservations.
// VI: Kiểm tra ưu tiên khoảng cách tới cửa vào qua điểm nối vòng và bỏ ô có món, không giữ chỗ từng phần.
const entranceData=level();entranceData.ingredientBoard[0].foods[0].requiredCount=3;
const entranceBelt=new LevelSimulation(entranceData,catalog);
assert(Math.abs(entranceBelt.distanceToStation(entranceBelt.slots[4])-.05)<1e-9);
assert(entranceBelt.distanceToStation(entranceBelt.slots[5])>.9);
assert(entranceBelt.pick(entranceBelt.dishes.find(d=>d.x===0&&d.y===0).id));
assert.deepEqual(entranceBelt.slots.filter(s=>s.item).map(s=>s.index),[2,3,4]);
const wrappedBelt=new LevelSimulation(level(),catalog);wrappedBelt.phase=.98;
assert(wrappedBelt.pick(wrappedBelt.dishes.find(d=>d.x===0&&d.y===0).id));
assert(wrappedBelt.slots[4].item,'Slot .38 is closer to entrance than slot .48 just past entrance');
const occupiedBelt=new LevelSimulation(level(),catalog);occupiedBelt.slots[4].item={type:2,state:'raw'};
assert(occupiedBelt.pick(occupiedBelt.dishes.find(d=>d.x===0&&d.y===0).id));
assert(occupiedBelt.slots[3].item);assert.equal(occupiedBelt.slots[4].item.type,2);
console.log('PASS: nearest upcoming station entrance, just-past entrance deferred, wraparound priority, occupied-slot exclusion and immediate reservation.');


// EN: Verifies full-belt preparation reserves its source and returns there even when a closer plate becomes empty.
// VI: Kiểm tra sơ chế trên băng đầy giữ đĩa nguồn và trả đúng đĩa dù có đĩa khác gần hơn trống.
const reserved=new LevelSimulation(level(),catalog);reserved.phase=.1;
for(const slot of reserved.slots)slot.item={type:2,state:'raw'};
reserved.updateChef();const portion=reserved.chefItem,source=reserved.slots[portion.destination];
assert.equal(source.item,portion);assert.equal(reserved.boardCapacity(),0);assert(!reserved.pick(reserved.dishes.find(d=>reserved.head(d)).id));
reserved.slots[0].item=null;for(let phase=0;phase<3;phase++){reserved.time=portion.readyAt;reserved.updateChef();}assert.equal(portion.state,'returning');assert.equal(source.item,portion);assert.equal(reserved.slots[0].item,null);
assert.equal(reserved.chefItem,portion);reserved.time=portion.readyAt;reserved.updateChef();assert.equal(portion.state,'prepared');assert.equal(source.item,portion);assert.notEqual(reserved.chefItem,portion);
const reset=new LevelSimulation(level(),catalog);assert.equal(reset.chefItem,null);assert.equal(reset.boardCapacity(),10);
console.log('PASS: occupied preparation source, full-belt pickup rejection, same-plate return despite other empty plates, prepared landing and reset.');
