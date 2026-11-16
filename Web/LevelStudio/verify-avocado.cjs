const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
vm.runInThisContext(fs.readFileSync('public/studio/lib/js-yaml.min.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/core.js','utf8'));vm.runInThisContext(fs.readFileSync('public/studio/play-engine.js','utf8'));
const catalog=JSON.parse(fs.readFileSync('public/studio/catalog.json','utf8'));
assert.equal(catalog.ingredients.find(x=>x.id===24).name,'Avocado');assert(fs.existsSync('public/studio/images/ingredient-24.png'));assert(fs.existsSync('public/studio/images/prepared-24.png'));assert.deepEqual(catalog.recipes.find(r=>r.finalFood===112).ingredients,[25,23]);
const level=Core.parseAsset(catalog.levels[0].yaml.value);level.data.ingredientBoard[0].foods[0].foodType=24;
const exported=Core.parseAsset(Core.exportAsset(level));assert.equal(exported.data.ingredientBoard[0].foods[0].foodType,24);assert(!Core.analyze(level.data,catalog).issues.some(s=>s.includes('24')));
const sim=new LevelSimulation(level.data,catalog);assert(sim.pick(sim.dishes.find(d=>d.foodType===24).id));sim.update(40);assert(sim.slots.some(s=>s.item?.type===24&&s.item.state==='prepared'));
console.log('PASS: avocado raw/prepared art registered, stable ID 24 survives .asset roundtrip, preparation works, yakitori remains chicken+sauce.');
