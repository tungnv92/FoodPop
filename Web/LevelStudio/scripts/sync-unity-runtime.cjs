const fs=require('fs'),path=require('path'),vm=require('vm'),crypto=require('crypto');
const project=path.resolve(__dirname,'../../..'),web=path.resolve(__dirname,'..'),ctx=vm.createContext({});
vm.runInContext(fs.readFileSync(path.join(web,'public/studio/lib/js-yaml.min.js'),'utf8'),ctx);
const manifest=JSON.parse(fs.readFileSync(path.join(web,'public/studio/unity/manifest.json')));
const configText=fs.readFileSync(path.join(project,'Assets/HotPot/Config/Resources/IngameConfig.asset'),'utf8');
const config=ctx.jsyaml.load(configText.slice(configText.indexOf('MonoBehaviour:'))).MonoBehaviour;
const geometry=JSON.parse(fs.readFileSync(path.join(web,'public/studio/unity/geometry.json')));
geometry.chef=JSON.parse(fs.readFileSync(path.join(web,'public/studio/unity/chef-point.json')));
const catalogPath=path.join(web,'public/studio/catalog.json'),catalog=JSON.parse(fs.readFileSync(catalogPath));
const components={};for(const c of manifest.components)components[c.type]=c.data;
let length=0;for(let i=0;i<geometry.belt.length;i++){const a=geometry.belt[i],b=geometry.belt[(i+1)%geometry.belt.length];length+=Math.hypot(a[0]-b[0],a[1]-b[1]);}
const sourceFiles=[];
// EN: Records hashes of gameplay sources so a later sync can identify local changes instead of assuming parity.
// VI: Ghi hash nguồn gameplay để lần đồng bộ sau nhận ra thay đổi local thay vì mặc định đã khớp.
function collect(dir){for(const file of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,file.name);if(file.isDirectory())collect(p);else if(p.endsWith('.cs'))sourceFiles.push({path:path.relative(project,p).replaceAll('\\','/'),sha256:crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex')});}}
collect(path.join(project,'Assets/HotPot/Scripts'));
const sprites={};for(const sprite of manifest.sprites){if(!fs.existsSync(path.join(web,'public/studio',sprite.url)))throw Error('Missing sprite '+sprite.url);sprites[sprite.name]??=sprite.url;}
catalog.unity={exportedFrom:manifest.scene||'Assets/HotPot/Scenes/HotPotDemo.unity',config,components,geometry,sprites,icons:JSON.parse(fs.readFileSync(path.join(web,'public/studio/unity/icons.json'))),audio:manifest.audio,sourceFiles,dishes:JSON.parse(fs.readFileSync(path.join(web,'public/studio/unity/dishes.json'))),failOnTimeout:fs.readFileSync(path.join(project,'Assets/HotPot/Scripts/gamebase/LevelController.cs'),'utf8').includes('else if (customerManager.ServeTimeExpired'),beltLength:length,beltSeconds:length/components.ConveyorChefMovement.speed};
catalog.gameplay.serveDelay=components.CustomerManager.serveDelayAfterLanding;
catalog.gameplay.liftDuration=config.preparation.liftDuration;catalog.gameplay.dropDuration=config.preparation.dropDuration;
fs.writeFileSync(catalogPath,JSON.stringify(catalog));
console.log('Unity runtime:',manifest.sprites.length,'sprites;',manifest.audio.length,'audio;',sourceFiles.length,'source fingerprints; belt',catalog.unity.beltSeconds.toFixed(3),'s');

for(const icon of catalog.unity.icons)for(const state of ['raw','prepared']){if(!icon[state]){console.log('Unassigned Unity icon:',icon.id,state);continue;}const source=path.join(web,'public/studio/unity/sprites/'+icon[state]+'.png');if(!fs.existsSync(source))throw Error('Missing ingredient sprite '+icon[state]);fs.copyFileSync(source,path.join(web,'public/studio/images/'+(state==='raw'?'ingredient-':'prepared-')+icon.id+'.png'));}

const modes={};for(const file of fs.readdirSync(path.join(project,'Assets/HotPot/Config/Resources/Levels'))){if(!file.endsWith('.asset'))continue;const text=fs.readFileSync(path.join(project,'Assets/HotPot/Config/Resources/Levels',file),'utf8');modes[path.basename(file,'.asset')]=Number(text.match(/difficultyMode: (\d+)/)?.[1]||0);}catalog.unity.levelModes=modes;fs.writeFileSync(catalogPath,JSON.stringify(catalog));
