const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
vm.runInThisContext(fs.readFileSync('public/studio/lib/js-yaml.min.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/core.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/play-engine.js','utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8')),recipe=catalog.recipes.find(r=>r.finalFood===112);
assert.deepEqual(recipe.ingredients,[25,23]);assert.equal(catalog.ingredients.find(i=>i.id===22).name,'ChickenThigh');assert.equal(catalog.ingredients.find(i=>i.id===25).name,'ChickenBites');assert.equal(catalog.ingredients.find(i=>i.id===23).name,'TeriyakiSauce');
for(const file of ['ingredient-22.png','prepared-22.png','ingredient-25.png','prepared-25.png','ingredient-23.png','prepared-23.png','dish-112.png'])assert(fs.existsSync('public/studio/images/'+file));
const level=Core.parseAsset(catalog.levels[0].yaml.value);Object.assign(level.data,{m_Name:'Yakitori verification',leftQueue:[],rightQueue:[],customerQueue:[{finalFoods:[112]}],boardColumns:2,boardRows:1,dishLinks:[],ingredientBoard:[{foods:[{...Core.emptyCell(),foodType:25}]},{foods:[{...Core.emptyCell(),foodType:23}]}]});
const report=Core.analyze(level.data,catalog);assert.equal(report.issues.length,0);assert.equal(report.totalNeed,2);assert.equal(report.totalHave,2);assert(report.balance.every(x=>x.delta===0));
const round=Core.parseAsset(Core.exportAsset(level));assert.deepEqual(Core.customerQueue(round.data)[0].finalFoods,[112]);assert.equal(round.data.ingredientBoard[1].foods[0].foodType,23);
const sim=new LevelSimulation(level.data,catalog);assert.equal(sim.target(23),null);assert(sim.target(25));assert.equal(sim.target(22),null,'Drumstick must not satisfy chicken-bites skewer recipe');assert(sim.pick(sim.dishes.find(d=>d.foodType===25).id));assert(sim.pick(sim.dishes.find(d=>d.foodType===23).id));
for(let i=0;i<6000&&!sim.won;i++)sim.update(.1);
assert(sim.won,'Chicken and sauce should prepare and serve the new recipe');assert.equal(sim.served,1);assert(sim.slots.every(s=>!s.item));assert.equal(sim.customer(sim.lanes[0]),null);
console.log('PASS: yakitori catalog/images, two-ingredient balance, .asset recipe/ingredient IDs, chicken-first service and complete preparation-to-customer simulation.');


assert.notDeepEqual(fs.readFileSync('public/studio/images/prepared-22.png'),fs.readFileSync('public/studio/images/prepared-25.png'),'Roasted drumstick and skewer must have different prepared art');
