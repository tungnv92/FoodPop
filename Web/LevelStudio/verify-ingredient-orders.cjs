const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
for(const f of ['lib/js-yaml.min.js','core.js','play-engine.js'])vm.runInThisContext(fs.readFileSync('public/studio/'+f,'utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8'));Core.setCatalog(catalog);
assert.deepEqual(Core.orderIngredients({finalFoods:[112]}),[[25,23]]);
const level=Core.parseAsset(catalog.levels[0].yaml.value);
Object.assign(level.data,{leftQueue:[],rightQueue:[],useUnifiedCustomerQueue:true,customerQueue:[{customerType:1,useIngredientOrders:true,ingredientOrders:[{ingredients:[25,24,25]}],finalFoods:[112]}],boardColumns:3,boardRows:1,dishLinks:[],ingredientBoard:[25,24,25].map(foodType=>({foods:[{...Core.emptyCell(),foodType}]}))});
assert.equal(Core.matchingRecipe([25,24,25]),undefined);
assert.deepEqual(Core.orderIngredients(level.data.customerQueue[0]),[[25,24,25]]);
assert(Core.analyze(level.data,catalog).balance.every(x=>x.delta===0));
for(const type of [0,1,2]){level.data.customerQueue[0].customerType=type;const round=Core.parseAsset(Core.exportAsset(level));assert.equal(round.data.customerQueue[0].customerType,type);assert.deepEqual(Core.orderIngredients(round.data.customerQueue[0]),[[25,24,25]]);const sim=new LevelSimulation(round.data,catalog);assert.equal(sim.customers[0].customerType,type);for(const dish of [...sim.dishes])assert(sim.pick(dish.id));for(let i=0;i<6000&&!sim.won;i++)sim.update(.1);assert(sim.won,'Unnamed group must serve all quantities');}
assert.deepEqual(Core.orderIngredients({useIngredientOrders:true,ingredientOrders:[],finalFoods:[112]}),[]);
assert.deepEqual(Core.customerQueue({useUnifiedCustomerQueue:true,customerQueue:[],leftQueue:[{}],rightQueue:[]}),[]);
const old={finalFoods:[112],_ingredientDrafts:{0:[24,25]}};Core.materializeOrder(old);assert.equal(old.customerType,0);assert.deepEqual(Core.orderIngredients(old),[[24,25]]);assert(!old._ingredientDrafts);
console.log('PASS: canonical ingredient quantities, unnamed group completion, Normal/Boss/Shipper roundtrip, empty-list priority and legacy conversion.');

for(const customerType of [1,2]){
 const customer={customerType,finalFoods:[112]};Core.materializeOrder(customer);assert.equal(customer.timeServe,30);
 level.data.customerQueue=[customer];customer.timeServe=45.5;
 const round=Core.parseAsset(Core.exportAsset(level));assert.equal(round.data.customerQueue[0].timeServe,45.5);
 assert.equal(new LevelSimulation(round.data,catalog).customers[0].timeServe,45.5);
 customer.timeServe=0;assert(Core.analyze(level.data,catalog).issues.some(x=>x.includes('Thời gian phục vụ')));
 customer.timeServe=-1;assert(Core.analyze(level.data,catalog).issues.some(x=>x.includes('Thời gian phục vụ')));
}
console.log('PASS: Boss/Shipper default 30 seconds, custom time roundtrip, simulation metadata and invalid time validation.');
