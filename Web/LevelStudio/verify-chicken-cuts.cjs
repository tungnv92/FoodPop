const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
for(const f of ['lib/js-yaml.min.js','core.js','play-engine.js'])vm.runInThisContext(fs.readFileSync('public/studio/'+f,'utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8'));Core.setCatalog(catalog);
for(const [id,name] of [[26,'ChickenWing'],[27,'ChickenBreast']]){
 assert.equal(catalog.ingredients.find(i=>i.id===id).name,name);
 for(const state of ['ingredient','prepared'])assert(fs.existsSync(`public/studio/images/${state}-${id}.png`));
 assert.notDeepEqual(fs.readFileSync(`public/studio/images/ingredient-${id}.png`),fs.readFileSync(`public/studio/images/prepared-${id}.png`));
 const level=Core.parseAsset(catalog.levels[0].yaml.value);Object.assign(level.data,{leftQueue:[],rightQueue:[],customerQueue:[{customerType:0,useIngredientOrders:true,ingredientOrders:[{ingredients:[id]}],finalFoods:[0]}],useUnifiedCustomerQueue:true,boardColumns:1,boardRows:1,dishLinks:[],ingredientBoard:[{foods:[{...Core.emptyCell(),foodType:id}]}]});
 assert.equal(Core.analyze(level.data,catalog).issues.length,0);assert(Core.analyze(level.data,catalog).balance.every(x=>!x.delta));
 const round=Core.parseAsset(Core.exportAsset(level));assert.equal(round.data.ingredientBoard[0].foods[0].foodType,id);assert.deepEqual(Core.orderIngredients(round.data.customerQueue[0]),[[id]]);
 const sim=new LevelSimulation(round.data,catalog);assert(sim.pick(sim.dishes[0].id));for(let n=0;n<6000&&!sim.won;n++)sim.update(.1);assert(sim.won);
}
console.log('PASS: wing/breast IDs, distinct raw/prepared art, balanced orders, Unity asset roundtrip and full preparation/service.');
