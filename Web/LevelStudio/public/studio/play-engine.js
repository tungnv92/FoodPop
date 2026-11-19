// EN: Wraps the deterministic web simulation without depending on browser rendering or Unity.
// VI: Đóng gói mô phỏng web xác định, không phụ thuộc trình vẽ hay Unity.
(function(root){
class LevelSimulation {
  // EN: Initializes board, belt and the chef reference to its reserved plate alongside customer seats.
  // VI: Khởi tạo board, băng và tham chiếu đầu bếp tới đĩa giữ chỗ cùng hai chỗ phục vụ.
  constructor(data,catalog,settings={}){
    this.tuning=catalog.gameplay||{actions:{},serveDelay:.2,liftDuration:.25,dropDuration:.3,poofDuration:.22};
    this.unity=catalog.unity;this.config=this.unity.config;this.state='Playing';this.bag=[];this.boost=null;this.preWin=false;this.departures=0;this.boardMovingUntil=0;
    this.configure(settings);
    this.chefItem=null;this.failed=null;this.idleSince=0;
    this.time=0;this.moves=0;this.served=0;this.stamp=0;this.phase=this.unity.components.ConveyorChefMovement.startOffset;this.events=[];this.flights=[];this.message='Chọn đĩa đang sáng ở đầu cột.';
    this.width=data.boardColumns;this.height=data.boardRows;this.dishes=[];this.slots=Array.from({length:this.unity.components.ConveyorChefMovement.slotCount},(_,i)=>({index:i,item:null}));
    for(let x=0;x<this.width;x++)for(let y=0;y<this.height;y++){const c=Core.cell(data,x,y);if(!c?.foodType||Core.covering(data,x,y))continue;this.dishes.push({...Core.clone(c),id:this.dishes.length,x,y,w:c.dishShape===2?2:1,h:c.dishShape===1?2:1,revealAt:null,group:null});}
    for(const [i,g] of (data.dishLinks||[]).entries())for(const [order,p] of g.dishes.entries()){const d=this.dishes.find(d=>d.x===p.x&&d.y===p.y);if(d){d.group=i;d.groupOrder=order;}}
    this.customers=Core.customerQueue(data).map((c,number)=>({number:number+1,customerType:c.customerType??0,timeServe:[1,2].includes(c.customerType)?(c.timeServe??30):null,timeLeft:[1,2].includes(c.customerType)?(c.timeServe??30):null,dishes:Core.orderIngredients(c,catalog).map(recipe=>{const id=Core.matchingRecipe(recipe,catalog)?.finalFood||0;return {id,recipe:[...recipe],missing:[...recipe],arrived:[],incoming:null,baseReady:false,done:false};})}));const queue=Core.customerQueue(data);for(let i=0;i<this.customers.length;i++)for(let j=0;j<this.customers[i].dishes.length;j++){const group=queue[i].ingredientOrders?.[j],guid=group?.dish?.guid;this.customers[i].dishes[j].picture=guid?this.unity.dishes.find(p=>p.guid===guid):null;}this.nextCustomer=0;this.lanes=[0,1].map(side=>({side,index:0,stamp:0,waitUntil:0,customers:[]}));this.total=this.customers.length;for(const lane of this.lanes)this.fillLane(lane);this.settle(false);this.reveal(true);
  }
  // EN: Assigns the next waiting customer to a free seat, preserving global order and arrival priority.
  // VI: Đưa khách chờ tiếp theo vào chỗ trống, giữ thứ tự chung và ưu tiên đến trước.
  fillLane(lane){if(this.time<lane.waitUntil||lane.customers[lane.index]||this.nextCustomer>=this.customers.length)return;lane.customers.push(this.customers[this.nextCustomer++]);lane.stamp=++this.stamp;}
  // EN: Validates seconds-per-loop tuning, migrates legacy percentages and preserves an in-progress action.
  // VI: Kiểm tra thông số giây/vòng, đổi dữ liệu phần trăm cũ và giữ action đang chạy.
  configure(settings={}){const seconds=Number(settings.beltSeconds??(settings.beltPercent?2000/settings.beltPercent:this.unity.beltSeconds)),action=Number(settings.actionSeconds??0);if(!Number.isFinite(seconds)||seconds<5||seconds>200||!Number.isFinite(action)||action<0||action>10)throw Error('Băng chuyền: 5–200 giây/vòng; action: 0–10 giây (0: theo animation).');this.settings={beltSeconds:seconds,actionSeconds:action};}
  // EN: Returns the live dish occupying a board position, including double-dish footprints.
  // VI: Trả về đĩa đang chiếm vị trí board, gồm vùng chiếm của đĩa đôi.
  at(x,y){return this.dishes.find(d=>x>=d.x&&x<d.x+d.w&&y>=d.y&&y<d.y+d.h);}
  // EN: Lists stable linked identities so movement never breaks a pick group.
  // VI: Liệt kê định danh trong nhóm để việc dồn không làm mất liên kết.
  members(d){return d.group==null?[d]:this.dishes.filter(a=>a.group===d.group).sort((a,b)=>a.groupOrder-b.groupOrder);}
  // EN: Requires the clicked dish at row zero across its entire width.
  // VI: Yêu cầu đĩa được bấm ở hàng đầu trên toàn bộ chiều rộng của nó.
  head(d){return d.y===0;}
  // EN: Checks that every cell above a linked member is empty or belongs to the same group.
  // VI: Kiểm tra các ô phía trên thành viên chỉ trống hoặc chứa đĩa cùng nhóm.
  exposed(group){return group.every(d=>{for(let x=d.x;x<d.x+d.w;x++)for(let y=0;y<d.y;y++){const a=this.at(x,y);if(a&&!group.includes(a))return false;}return true;});}
  // EN: Compacts complete dish footprints upward repeatedly, waiting for both columns of horizontal dishes.
  // VI: Dồn nguyên vùng đĩa lên nhiều bước, đĩa ngang chờ cả hai cột cùng trống.
  settle(animate=true){const starts=new Map(this.dishes.map(d=>[d.id,d.y]));let moved;do{moved=false;for(const d of [...this.dishes].sort((a,b)=>a.x-b.x||a.y-b.y)){if(d.y<1)continue;let free=true;for(let x=d.x;x<d.x+d.w;x++)if(this.at(x,d.y-1))free=false;if(free){d.y--;moved=true;}}}while(moved);if(animate&&this.dishes.some(d=>starts.get(d.id)!==d.y))this.boardMovingUntil=this.time+this.config.boardMotion.shiftUpDuration;}
  // EN: Starts hidden-lid reveals when a dish or its linked group becomes exposed.
  // VI: Bắt đầu mở nắp ẩn khi đĩa hoặc nhóm liên kết được lộ ra.
  reveal(immediate=false){for(const d of this.dishes)if(d.isHidden&&d.revealAt==null&&(this.head(d)||(d.group!=null&&this.exposed(this.members(d))))){if(immediate)d.isHidden=0;else d.revealAt=Math.max(this.time,this.boardMovingUntil)+this.config.lidReveal.fadeDuration;}}
  // EN: Expands each unique linked dish into individual portions, retaining its half-specific board origin.
  // VI: Tách mỗi đĩa trong nhóm nối thành các phần, giữ vị trí riêng của từng nửa đĩa.
  portions(group){const result=[];for(const d of new Set(group)){for(let i=0;i<d.requiredCount;i++)result.push({type:d.foodType,origin:{x:d.x+.5,y:d.y+.5}});if(d.dishShape)for(let i=0;i<d.secondCount;i++)result.push({type:d.secondFoodType,origin:{x:d.x+(d.w===2?1.5:.5),y:d.y+(d.h===2?1.5:.5)}});}return result;}
  // EN: Evaluates atomic capacity without reserving: at most one overflow portion may use an idle chef and an accepting order.
  // VI: Xét sức chứa cả nhóm mà chưa giữ chỗ: tối đa một phần dư được đi thẳng qua đầu bếp rảnh tới order nhận được.
  pickPlan(d){const portions=this.portions(this.members(d)),free=this.boardCapacity();if(portions.length<=free)return {portions,direct:-1};if(portions.length>free+1)return {error:'Tổng phần vượt số slot trống cộng một phần đi thẳng.'};if(this.chefItem)return {error:'Thiếu một slot và đầu bếp đang bận.'};const direct=portions.findIndex(p=>this.target(p.type));return direct<0?{error:'Thiếu một slot và không order nào nhận phần dư.'}:{portions,direct};}
  // EN: Rejects blocked or paused picks using the same capacity plan that commits the whole group.
  // VI: Từ chối nhóm bị chặn hoặc lượt dừng, dùng chung kế hoạch sức chứa với lúc lấy cả nhóm.
  rejection(d,ignoreDepth=false){if(this.boost)return 'Booster đang chạy.';if(this.failed)return 'Lượt chơi đã dừng.';if(!d)return 'Ô này không có đĩa.';if(!ignoreDepth&&!this.head(d))return 'Chỉ chọn đĩa ở đầu cột.';const group=this.members(d);if(!ignoreDepth&&!this.exposed(group))return 'Nhóm link còn bị đĩa khác chặn phía trên.';if(group.some(d=>d.isFrozen&&d.blockValue>0))return 'Đĩa đang đóng băng.';if(group.some(d=>d.isHidden))return 'Đang mở nắp, chờ một chút.';return this.pickPlan(d).error||'';}
  // EN: Reserves a single order ingredient immediately so belt service cannot dispatch a duplicate during direct preparation.
  // VI: Giữ ngay một nguyên liệu order để băng không gửi trùng trong thời gian sơ chế đi thẳng.
  reserve(type,target,from){const dish=target.dish;dish.missing.splice(dish.missing.indexOf(type),1);const flight={type,start:this.time,end:Infinity,from,side:target.lane.side,dish:target.index};dish.incoming=flight;return flight;}
  // EN: Commits all portions, retaining source slots or reserving exactly one overflow portion for direct service.
  // VI: Chốt mọi phần, giữ slot nguồn hoặc giữ đúng một phần dư để phục vụ thẳng.
  pick(id,ignoreDepth=false){const d=this.dishes.find(d=>d.id===id),reason=this.rejection(d,ignoreDepth);if(reason){this.message=reason;return false;}const group=this.members(d),plan=this.pickPlan(d),free=this.slots.filter(s=>!s.item).sort((a,b)=>this.distanceToStation(a)-this.distanceToStation(b)),picked=[];let next=0;
    for(const [index,p] of plan.portions.entries()){const delay=plan.portions.slice(0,index).filter(v=>v.origin.x===p.origin.x&&v.origin.y===p.origin.y).length*this.config.boardFlight.portionDelay;const item={...p,state:'arriving',start:this.time+delay,readyAt:this.time+delay+this.config.boardFlight.flyDuration};if(index===plan.direct){item.direct=this.reserve(item.type,this.target(item.type),null);this.beginPreparation(item,null);}else{const slot=free[next++];slot.item=item;picked.push(slot);}}
    if(!this.chefItem){const best=picked.slice().sort((a,b)=>this.needRank(a.item.type)-this.needRank(b.item.type))[0];const rank=best?this.needRank(best.item.type):Infinity;const waiting=this.slots.some(slot=>!picked.includes(slot)&&slot.item?.state==='raw'&&this.needRank(slot.item.type)<=rank);if(best&&!waiting)this.beginPreparation(best.item,best.index);}
    this.dishes=this.dishes.filter(d=>!group.includes(d));this.settle();for(const a of this.dishes)if(a.isFrozen&&a.blockValue>0){a.blockValue--;if(a.blockValue===0)a.isFrozen=0;}this.reveal();this.moves++;this.events.push('pick');this.message='Đã lấy '+plan.portions.length+' phần'+(plan.direct>=0?' · một phần đi thẳng tới order.':'.');return true;
  }
  // EN: Measures forward travel to the clockwise station entrance; slots just past it wait almost a full loop.
  // VI: Tính quãng chạy tới cửa vào trạm theo chiều kim đồng hồ; ô vừa qua cửa phải chờ gần một vòng.
  distanceToStation(slot){return (((this.unity.components.ConveyorChefMovement.clockwise?this.unity.components.ConveyorChefMovement.stationStart:this.unity.components.ConveyorChefMovement.stationEnd)-this.position(slot))*(this.unity.components.ConveyorChefMovement.clockwise?1:-1)%1+1)%1;}
  // EN: Computes evenly spaced clockwise slot positions on the circulating belt.
  // VI: Tính vị trí chuẩn hóa của slot theo chiều chạy băng chuyền.
  position(slot){return (slot.index/this.slots.length+this.phase)%1;}
  // EN: Counts only empty plates; the chef's original plate remains occupied until serving.
  // VI: Chỉ đếm đĩa trống; đĩa gốc của đầu bếp vẫn có món đến khi phục vụ.
  boardCapacity(){return this.slots.filter(s=>!s.item).length;}
  // EN: Ranks ingredients by customer arrival, card order and recipe order, including unserved belt food.
  // VI: Xếp nguyên liệu theo khách đến trước, thứ tự món và công thức, tính cả món chưa phục vụ trên băng.
  needRank(type){let rank=0;for(const lane of [...this.lanes].sort((a,b)=>a.stamp-b.stamp)){const c=this.customer(lane);if(!c)continue;for(const dish of c.dishes)if(!dish.done)for(const id of dish.recipe)if(dish.missing.includes(id)){if(id===type)return rank;rank++;}}return Infinity;}
  // EN: Uses the local animation duration unless the playtest explicitly overrides it.
  // VI: Dùng thời lượng animation local, trừ khi thiết lập chơi thử chủ động ghi đè.
  actionDuration(type){return this.settings.actionSeconds||this.tuning.actions[type]?.duration||1.7667;}
  // EN: Starts the incoming hop and marks the chef busy immediately, with or without a reserved belt plate.
  // VI: Bắt đầu bay tới thớt và đánh dấu đầu bếp bận ngay, có hoặc không có đĩa băng giữ chỗ.
  beginPreparation(item,destination){this.chefItem=item;Object.assign(item,{state:'lifting',destination,start:this.time,readyAt:this.time+this.flightDuration(this.tuning.liftDuration,item.origin?this.boardWorld(item.origin):this.beltWorld(this.position(this.slots[destination])))});}
  // EN: Advances lift, one-shot action, poof and return; landed raw food anywhere competes by need then station distance.
  // VI: Chạy bay vào, action một lượt, poof và bay về; món thô đã đáp ở mọi vị trí ưu tiên theo nhu cầu rồi khoảng cách trạm.
  updateChef(){const item=this.chefItem;if(item&&this.time>=item.readyAt){if(item.state==='lifting'){item.state='preparing';item.started=this.time;item.readyAt=this.time+this.actionDuration(item.type);}else if(item.state==='preparing'){item.state='poof';item.readyAt=this.time+this.tuning.poofDuration;}else if(item.state==='poof'){if(item.direct){Object.assign(item.direct,{start:this.time,end:this.time+this.config.ingredientFlight.serveFlyDuration});this.flights.push(item.direct);this.chefItem=null;this.idleSince=this.time;}else{item.state='returning';item.start=this.time;item.readyAt=this.time+this.flightDuration(this.tuning.dropDuration,this.beltWorld(this.position(this.slots[item.destination])));item.returnDuration=item.readyAt-this.time;}}else if(item.state==='returning'){item.state='prepared';item.landedAt=this.time;this.chefItem=null;this.idleSince=this.time;}}
    if(this.chefItem)return;const candidates=this.slots.filter(s=>s.item?.state==='raw').sort((a,b)=>this.needRank(a.item.type)-this.needRank(b.item.type)||this.chefDistance(a)-this.chefDistance(b));if(candidates.length)this.beginPreparation(candidates[0].item,candidates[0].index);
  }
  // EN: Prefers a slot about to leave the preparation segment, then the nearest future entrance.
  // VI: Ưu tiên slot sắp ra khỏi trạm sơ chế, rồi slot sắp tới cửa vào nhất.
  chefDistance(slot){const p=this.position(slot),c=this.unity.components.ConveyorChefMovement,a=c.stationStart,b=c.stationEnd,len=(b-a+1)%1,progress=(p-a+1)%1;return progress<=len?-1-(c.clockwise?progress/len:1-progress/len):this.distanceToStation(slot);}
  // EN: Maps belt progress to the same rounded canvas path used for rendering and return selection.
  // VI: Đổi tiến độ băng sang đường canvas bo góc dùng chung khi vẽ và chọn đĩa trả.
  static beltPoint(p){const radius=45,straight=670,vertical=70,arc=Math.PI*radius/2,total=2*straight+2*vertical+4*arc;let distance=(((p%1)+1)%1*total+straight/2)%total;const sections=[['line',straight,145,65,815,65],['arc',arc,815,110,-Math.PI/2,0],['line',vertical,860,110,860,180],['arc',arc,815,180,0,Math.PI/2],['line',straight,815,225,145,225],['arc',arc,145,180,Math.PI/2,Math.PI],['line',vertical,100,180,100,110],['arc',arc,145,110,Math.PI,Math.PI*1.5]];for(const [kind,length,x,y,a,b] of sections){if(distance<=length){const t=distance/length;if(kind==='line')return [x+(a-x)*t,y+(b-y)*t];const angle=a+(b-a)*t;return [x+Math.cos(angle)*radius,y+Math.sin(angle)*radius];}distance-=length;}return [480,65];}
  // EN: Resolves the current customer only after the preceding customer's departure finishes.
  // VI: Lấy khách hiện tại sau khi khách trước rời đi xong.
  customer(lane){return lane.departAt==null&&this.time>=lane.waitUntil?lane.customers[lane.index]:null;}
  // EN: Selects the earliest arrived eligible lane and its first accepting dish, enforcing base-first and one flight per dish.
  // VI: Chọn làn đến trước và món nhận được đầu tiên, bắt buộc đế trước và mỗi món một chuyến bay.
  target(type){for(const lane of [...this.lanes].sort((a,b)=>a.stamp-b.stamp)){const customer=this.customer(lane);if(!customer)continue;for(const [index,dish] of customer.dishes.entries())if(!dish.done&&!dish.incoming&&dish.missing.includes(type)&&(dish.baseReady||type===dish.recipe[0]))return {lane,dish,index};}return null;}
  // EN: Advances bounded deterministic time, completing flights, chef jobs, automatic serving and lane transitions.
  // VI: Tiến thời gian xác định theo bước nhỏ, hoàn tất bay, sơ chế, tự phục vụ và chuyển khách.
  update(dt){if(dt<=0||this.failed||this.state==='Won'||this.state==='Lost')return;for(let left=dt*(this.preWin?this.config.preWin.speedMultiplier:1);left>1e-8;left-=.025){if(this.failed)break;this.step(Math.min(.025,left));}}
  // EN: Advances reserved chef jobs, landings and serving without releasing preparation plates.
  // VI: Chạy sơ chế giữ chỗ, lượt đáp và phục vụ mà không trả trống đĩa đang sơ chế.
  step(dt){this.time+=dt;for(const lane of this.lanes){const c=this.customer(lane);if(c?.timeLeft!=null&&!c.dishes.every(d=>d.done)){c.timeLeft=Math.max(0,c.timeLeft-dt);if(c.timeLeft===0&&this.unity.failOnTimeout){this.failed='timeout';return;}}}if(this.boost?.type==='clean'){if(this.chefItem){this.chefItem.readyAt+=dt;if(this.chefItem.started!=null)this.chefItem.started+=dt;}this.updateBooster();this.updateCustomers();return;}if(this.boost)this.updateBooster();for(const lane of this.lanes)this.fillLane(lane);this.phase=(this.phase+(this.unity.components.ConveyorChefMovement.clockwise?1:-1)*dt/this.settings.beltSeconds+1)%1;for(const d of this.dishes)if(d.isHidden&&d.revealAt!=null&&this.time>=d.revealAt){d.isHidden=0;d.revealAt=null;}
    for(const s of this.slots)if(s.item?.state==='arriving'&&this.time>=s.item.readyAt){s.item.state=s.item.returnState||'raw';s.item.landedAt=this.time;delete s.item.origin;delete s.item.fromBag;}
    this.updateChef();
    this.updateCustomers();
    this.flights=this.flights.filter(f=>f.end>this.time);for(const s of this.slots){if(s.item?.state!=='prepared')continue;if(this.time-(s.item.landedAt??0)<this.tuning.serveDelay)continue;const p=this.position(s);const target=this.target(s.item.type);if(!target)continue;const item=s.item,dish=target.dish;dish.missing.splice(dish.missing.indexOf(item.type),1);const flight={type:item.type,start:this.time,end:this.time+this.config.ingredientFlight.serveFlyDuration,from:p,side:target.lane.side,dish:target.index};dish.incoming=flight;this.flights.push(flight);s.item=null;this.events.push('serve');}
    this.returnFromBag();this.checkOutcome();
  }
  // EN: Interpolates the actual Unity spline samples in world coordinates.
  // VI: Nội suy mẫu spline Unity thật bằng tọa độ world.
  beltWorld(p){const points=this.unity.geometry.belt,index=((p%1+1)%1)*points.length,i=Math.floor(index),a=points[i],b=points[(i+1)%points.length],t=index-i;return [a[0]+(b[0]-a[0])*t,a[1]+(b[1]-a[1])*t];}
  // EN: Resolves a portion origin using current column spacing and the board's authored row spacing.
  // VI: Tính nguồn phần món theo khoảng cột hiện tại và khoảng hàng board được cấu hình.
  boardWorld(origin){const c=this.unity.components.FoodBoardManager;return [(origin.x-this.width/2)*c.columnSpacing,this.unity.geometry.columns[0][1]-(origin.y-.5)*c.rowSpacing];}
  // EN: Matches ChefManager's distance extension and maximum flight multiplier.
  // VI: Khớp cách ChefManager tăng thời gian bay theo khoảng cách và giới hạn hệ số tối đa.
  flightDuration(base,point){const c=this.config.preparation,chef=this.unity.geometry.chef,distance=Math.hypot(point[0]-chef[0],point[1]-chef[1]);return Math.min(base+Math.max(0,distance-c.nearFlightDistance)*c.flightTimePerUnit,base*Math.max(1,c.maxFlightMultiplier));}
  // EN: Lists current customer needs in arrival/card/recipe order, optionally subtracting all reserved belt portions.
  // VI: Liệt kê nhu cầu theo khách đến, thứ tự món/công thức, tùy chọn trừ mọi phần đã giữ trên băng.
  needed(excludeBelt=true){const needs=[];for(const lane of [...this.lanes].sort((a,b)=>a.stamp-b.stamp)){const c=this.customer(lane);if(c)for(const dish of c.dishes)if(!dish.done)for(const id of dish.recipe)if(dish.missing.includes(id))needs.push(id);}if(excludeBelt)for(const slot of this.slots)if(slot.item){const i=needs.indexOf(slot.item.type);if(i>=0)needs.splice(i,1);}return needs;}
  // EN: Advances ingredient landings, prefab-specific preparation and dish delivery before customer departure and queue stepping.
  // VI: Chạy đáp nguyên liệu, chế biến riêng theo prefab và giao món trước khi khách rời đi và hàng chờ tiến lên.
  updateCustomers(){for(const lane of this.lanes){if(lane.departAt!=null){if(this.time<lane.departAt)continue;this.served++;lane.index++;lane.departAt=null;lane.waitUntil=this.time+(this.nextCustomer<this.total?this.config.customers.stepForwardDuration:0);this.events.push('customer');continue;}const c=this.customer(lane);if(!c)continue;for(const dish of c.dishes){if(dish.incoming&&this.time>=dish.incoming.end){dish.arrived.push(dish.incoming.type);if(dish.incoming.type===dish.recipe[0])dish.baseReady=true;dish.incoming=null;if(!dish.missing.length)dish.finishAt=this.time+this.assemblyDuration(dish);}if(dish.finishAt&&this.time>=dish.finishAt)dish.done=true;}if(!c.dishes.length||c.dishes.every(d=>d.done)){lane.departAt=this.time+this.config.customers.leaveDelay+this.unity.components.OrderView.cardHideDuration+this.config.customers.slideDuration;}}}
  // EN: Uses actual final-dish clips and shared settling/merge/order-flight timing; unpictured groups only settle and mark complete.
  // VI: Dùng clip món thật và thời gian ổn định/ghép/bay order chung; nhóm không có hình chỉ ổn định rồi hoàn tất.
  assemblyDuration(dish){const a=this.config.dishAssembly,f=this.config.dishOrderFlight;return Math.max(this.config.trayStack.bounceDuration+this.config.trayStack.bounceHold,dish.picture?.duration||0)+a.assembleDelay+(dish.picture?a.mergeDuration+a.dishPopDuration+f.dishFlyDelay+f.dishFlyDuration:0);}
  // EN: Defers loss while any customer, board animation, chef or ingredient flight can progress, then uses the same pick planner for deadlock.
  // VI: Hoãn thua khi khách, hoạt cảnh board, đầu bếp hoặc món bay còn tiến triển, rồi dùng cùng kế hoạch lấy để xét bế tắc.
  checkOutcome(){if(this.won){this.state='Won';this.events.push('win');return;}if(!this.preWin&&this.portions(this.dishes).length<=this.boardCapacity())this.preWin=true;if(this.chefItem||this.boost||this.boardMovingUntil>this.time||this.dishes.some(d=>d.revealAt!=null)||this.flights.length||this.lanes.some(l=>l.departAt!=null||this.time<l.waitUntil))return;for(const slot of this.slots)if(slot.item&&(slot.item.state==='arriving'||this.target(slot.item.type)))return;if(this.customers.some(c=>c.dishes.some(d=>d.incoming||!d.done&&d.missing.length===0)))return;if(this.slots.some(s=>s.item)&&!this.dishes.some(d=>!this.rejection(d))){this.state='ReviveOffered';this.failed='capacity';this.message='Không còn nhóm lấy hợp lệ · hồi sinh để thu món vào túi.';this.events.push('blocked');}}
  // EN: Collects belt portions into storage while preserving an active chef's owned portion; no ingredients are destroyed.
  // VI: Thu phần món trên băng vào túi, giữ phần đầu bếp đang xử lý; không hủy nguyên liệu.
  clean(revive=false){if(this.boost||this.state==='Lost'||!this.slots.some(s=>s.item&&s.item!==this.chefItem))return false;const c=this.unity.components.CleanBrushBooster,items=[];for(const slot of this.slots)if(slot.item&&slot.item!==this.chefItem){items.push(slot.item);slot.item=null;}this.boost={type:'clean',items,ends:this.time+c.liftDuration+c.flyDuration+c.collectInterval*Math.max(0,items.length-1),revive};this.failed=null;this.state=revive?'Reviving':'Playing';return true;}
  // EN: Confirms defeat only when the pending revive is declined.
  // VI: Chỉ xác nhận thua khi từ chối lời mời hồi sinh đang chờ.
  declineRevive(){if(this.state==='ReviveOffered'){this.state='Lost';this.failed='lost';this.events.push('lose');}}
  // EN: Returns one currently acceptable stored portion into a reserved empty slot, preserving its raw/prepared state.
  // VI: Trả một phần trong túi mà khách nhận được vào slot trống đã giữ, giữ trạng thái thô/đã sơ chế.
  returnFromBag(){if(this.boost||this.slots.some(s=>s.item?.fromBag))return;for(const type of this.needed()){const index=this.bag.findIndex(p=>p.type===type);if(index<0||!this.target(type))continue;const slot=this.slots.find(s=>!s.item);if(!slot)return;const portion=this.bag.splice(index,1)[0],c=this.unity.components.FoodHoldingStorage;slot.item={type,state:'arriving',returnState:portion.state,start:this.time,readyAt:this.time+c.riseDuration+c.returnDuration,fromBag:true};return;}}
  // EN: Applies the completed booster atomically, then restores ordinary play and lid reveals.
  // VI: Áp dụng booster hoàn tất đồng thời, rồi trả về chơi thường và mở nắp.
  updateBooster(){if(this.time<this.boost.ends)return;const job=this.boost;this.boost=null;if(job.type==='clean')for(const item of job.items)this.bag.push({type:item.type,state:item.state==='prepared'?'prepared':'raw'});if(job.type==='shuffle')this.applyShuffle(job.seed);if(job.type==='pickup'){for(const d of this.members(job.dish)){d.isHidden=0;d.revealAt=null;}this.pick(job.dish.id,true);}if(job.type==='auto')for(const type of job.types){job.dish.arrived.push(type);if(type===job.dish.recipe[0])job.dish.baseReady=true;}if(job.type==='auto'){job.dish.incoming=null;job.dish.finishAt=this.time+this.assemblyDuration(job.dish);}this.state='Playing';this.failed=null;this.reveal();}
  // EN: Selects a hidden/deeper whole group within Pickup's visible-row limit; ice and combined capacity remain mandatory.
  // VI: Chọn cả nhóm ẩn/sâu trong giới hạn hàng Pickup; vẫn bắt buộc kiểm tra băng và tổng sức chứa.
  pickup(id){const dish=this.dishes.find(d=>d.id===id),limit=this.unity.components.PickupHandBooster.maxVisibleRows;if(!dish||this.boost||this.failed||!this.dishes.some(d=>d.y+d.h>=2))return false;const members=this.members(dish);if(members.some(d=>d.y+d.h>limit||d.isFrozen)||this.pickPlan(dish).error)return false;this.boost={type:'pickup',dish,ends:this.time+this.unity.components.PickupHandBooster.liftDuration+this.unity.components.PickupHandBooster.revealDuration};return true;}
  // EN: Starts the local Shuffle phases; every double footprint stays fixed while single dishes move with their links.
  // VI: Bắt đầu các pha Shuffle local; mọi vùng đĩa đôi giữ nguyên, đĩa đơn di chuyển cùng liên kết.
  shuffle(seed=Date.now()){if(this.failed||this.boost||this.dishes.filter(d=>!d.dishShape).length<2)return false;const c=this.unity.components.ShuffleBooster;this.boost={type:'shuffle',seed,ends:this.time+c.glowDuration+c.lidCloseDuration+c.shakeDuration+c.swapDuration+c.lidOpenDuration};return true;}
  // EN: Places urgent ingredients in unfrozen first/second-row spots, then shuffles remaining singles, preserving each spot's hidden/ice flags.
  // VI: Đặt nguyên liệu cần gấp vào vị trí không băng ở hàng một/hai, rồi xáo đĩa đơn còn lại, giữ cờ ẩn/băng từng vị trí.
  applyShuffle(seed){let value=seed>>>0;const random=()=>{value=(Math.imul(value,1664525)+1013904223)>>>0;return value/4294967296;};const singles=this.dishes.filter(d=>!d.dishShape),spots=singles.map(d=>({x:d.x,y:d.y,isHidden:d.isHidden,isFrozen:d.isFrozen,blockValue:d.blockValue})),remaining=[...singles],order=new Map(),needs=this.needed();let nextNeed=0;for(const row of [0,1]){let places=spots.filter(p=>p.y===row&&!p.isFrozen&&!p.isHidden);if(!places.length)places=spots.filter(p=>p.y===row&&!p.isFrozen);for(let n=nextNeed;n<needs.length&&places.length;n++){const matches=remaining.filter(d=>d.foodType===needs[n]);if(!matches.length)continue;const d=matches[Math.floor(random()*matches.length)],spot=places[Math.floor(random()*places.length)];order.set(spot,d);remaining.splice(remaining.indexOf(d),1);nextNeed=n+1;break;}}for(let i=remaining.length-1;i>0;i--){const j=Math.floor(random()*(i+1));[remaining[i],remaining[j]]=[remaining[j],remaining[i]];}for(const spot of spots){const d=order.get(spot)||remaining.pop();Object.assign(d,spot,{revealAt:null});}this.settle();}
  // EN: Completes the earliest reservable dish using real portions in belt, bag and board priority, including hidden and frozen board portions.
  // VI: Hoàn thành món có thể giữ sớm nhất bằng phần thật, ưu tiên băng, túi, board, gồm cả phần ẩn và đóng băng.
  autoComplete(){if(this.failed||this.boost)return false;let chosen;for(const lane of [...this.lanes].sort((a,b)=>a.stamp-b.stamp)){const customer=this.customer(lane);if(!customer)continue;chosen=customer.dishes.find(d=>!d.done&&!d.incoming&&d.missing.length);if(chosen)break;}if(!chosen)return false;const types=[...chosen.missing];for(const type of types){const slot=this.slots.find(s=>s.item?.type===type);if(slot){if(slot.item===this.chefItem)this.chefItem=null;slot.item=null;continue;}const bag=this.bag.findIndex(p=>p.type===type);if(bag>=0){this.bag.splice(bag,1);continue;}this.takeBoardPortion(type);}chosen.missing=[];chosen.incoming={type:0,end:Infinity};const c=this.unity.components.AutoCompleteBooster;this.boost={type:'auto',dish:chosen,types,ends:this.time+c.appearDuration+c.gatherInterval*types.length+c.gatherDuration+c.shineHold+c.flyDuration+c.vanishDuration+c.popDuration+c.dropInterval*Math.max(0,types.length-1)+this.config.ingredientFlight.serveFlyDuration};return true;}
  // EN: Removes one real board portion, opening its lid and breaking ice; exhausted linked members reconnect or dissolve.
  // VI: Lấy một phần thật trên board, mở nắp và phá băng; thành viên nối hết phần được gỡ và nối lại hoặc giải thể.
  takeBoardPortion(type){const options=this.dishes.filter(d=>d.foodType===type&&d.requiredCount>0||d.secondFoodType===type&&d.secondCount>0&&d.dishShape).sort((a,b)=>Number(a.isFrozen)-Number(b.isFrozen)||a.x-b.x||a.y-b.y),dish=options[0];if(!dish)return false;dish.isHidden=0;dish.isFrozen=0;if(dish.foodType===type&&dish.requiredCount>0)dish.requiredCount--;else dish.secondCount--;if(dish.requiredCount+(dish.dishShape?dish.secondCount:0)===0){const group=dish.group;this.dishes.splice(this.dishes.indexOf(dish),1);const left=this.dishes.filter(d=>d.group===group);if(group!=null&&left.length===1)left[0].group=null;}this.settle();this.reveal();return true;}
  // EN: Reports victory only after every customer's dishes have arrived and been assembled.
  // VI: Chỉ báo thắng khi món của mọi khách đã nhận đủ nguyên liệu và ghép xong.
  get won(){return this.total>0&&this.served===this.total;}
}
root.LevelSimulation=LevelSimulation;
})(globalThis);
