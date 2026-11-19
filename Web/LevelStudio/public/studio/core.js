// EN: Deep-copies serializable level data before edits and exports.
// VI: Sao chép dữ liệu level trước khi chỉnh và xuất.
function clone(value){return JSON.parse(JSON.stringify(value));}
let orderCatalog={recipes:[]};
// EN: Supplies recipe metadata for legacy imports; explicit ingredient lists never depend on it.
// VI: Cấp dữ liệu công thức để nhập level cũ; danh sách nguyên liệu trực tiếp không phụ thuộc dữ liệu này.
function setCatalog(catalog){orderCatalog=catalog;}
// EN: Resolves explicit ingredient groups, then old browser drafts or final-food recipes as a compatibility fallback.
// VI: Lấy nhóm nguyên liệu trực tiếp, rồi mới dùng nháp trình duyệt hoặc công thức món cũ để tương thích.
function orderIngredients(customer,catalog=orderCatalog){if(customer.useIngredientOrders||customer.ingredientOrders?.length)return (customer.ingredientOrders||[]).map(g=>unpack(g.ingredients));return (customer.finalFoods||[]).map((id,j)=>[...(customer._ingredientDrafts?.[j]??catalog.recipes.find(r=>r.finalFood===id)?.ingredients??[])]);}
// EN: Finds optional dish metadata by exact ingredient quantities without changing the selected base order.
// VI: Tìm thông tin món tùy chọn theo đúng lượng nguyên liệu, không đổi thứ tự nguyên liệu đế đã chọn.
function matchingRecipe(items,catalog=orderCatalog){const key=[...items].sort((a,b)=>a-b).join(',');return items.length?catalog.recipes.find(r=>[...r.ingredients].sort((a,b)=>a-b).join(',')===key):undefined;}
// EN: Stores authoritative ingredients and regenerates final-food hints for the existing editor controls.
// VI: Lưu nguyên liệu chính và tạo lại gợi ý món để dùng với bộ chỉnh sửa hiện tại.
function setOrderGroups(customer,groups,catalog=orderCatalog){customer.useIngredientOrders=true;customer.ingredientOrders=groups.map(items=>({ingredients:[...items]}));customer.finalFoods=groups.map(items=>matchingRecipe(items,catalog)?.finalFood||0);delete customer._ingredientDrafts;}
// EN: Converts old orders into explicit groups and defaults legacy customer type and special-customer serving time.
// VI: Chuyển order cũ thành nhóm trực tiếp và đặt mặc định loại khách cũ và thời gian phục vụ khách đặc biệt.
function materializeOrder(customer,catalog=orderCatalog){customer.customerType??=0;if(customer.customerType===1||customer.customerType===2)customer.timeServe??=30;setOrderGroups(customer,orderIngredients(customer,catalog),catalog);}
// EN: Uses the shared queue and direct ingredients as the saved level contract.
// VI: Dùng hàng chung và nguyên liệu trực tiếp làm cấu trúc level được lưu.
function materializeLevel(d,catalog=orderCatalog){d.customerQueue=customerQueue(d);d.useUnifiedCustomerQueue=true;for(const c of d.customerQueue)materializeOrder(c,catalog);}
// EN: Reads Unity's little-endian packed enum arrays without losing leading zeros.
// VI: Đọc mảng enum đóng gói little-endian của Unity, giữ số không đầu.
function unpack(value){if(Array.isArray(value))return value.map(Number);if(value==null||value==='')return [];const s=String(value);if(!/^(?:[0-9a-fA-F]{8})+$/.test(s))throw Error('Mảng finalFoods không đúng định dạng Unity.');return s.match(/.{8}/g).map(hex=>parseInt(hex.match(/../g).reverse().join(''),16));}
// EN: Packs enum IDs as Unity-compatible little-endian bytes.
// VI: Đóng gói mã enum thành byte little-endian tương thích Unity.
function pack(values){return values.map(id=>Number(id).toString(16).padStart(8,'0').match(/../g).reverse().join('')).join('');}
// EN: Imports both old dish IDs and new packed ingredient lists from shared or legacy Unity queues.
// VI: Nhập cả ID món cũ và danh sách nguyên liệu đóng gói mới từ hàng chung hoặc hàng Unity cũ.
function parseAsset(text){if(text.length>2000000)throw Error('File vượt giới hạn 2 MB.');const match=text.match(/^(%YAML[^]*?--- !u!114 &\d+\r?\n)/);if(!match)throw Error('Cần file LevelConfig .asset dạng text của Unity.');const body=text.slice(match[0].length).replace(/((?:finalFoods|ingredients):\s*)([0-9a-fA-F]+)(?=\s*(?:\n|$))/g,'$1"$2"');const parsed=jsyaml.load(body),d=parsed?.MonoBehaviour;if(!d||!Array.isArray(d.ingredientBoard)||(!Array.isArray(d.customerQueue)&&(!Array.isArray(d.leftQueue)||!Array.isArray(d.rightQueue))))throw Error('File không phải LevelConfig được hỗ trợ.');d.leftQueue??=[];d.rightQueue??=[];if(!Number.isInteger(d.boardColumns)||!Number.isInteger(d.boardRows)||d.boardColumns<1||d.boardColumns>12||d.boardRows<1||d.boardRows>100)throw Error('Kích thước board không hợp lệ (tối đa 12 × 100).');for(const q of [d.leftQueue,d.rightQueue,...(d.customerQueue?[d.customerQueue]:[])])for(const c of q){c.finalFoods=unpack(c.finalFoods);if(c.ingredientOrders)for(const g of c.ingredientOrders)g.ingredients=unpack(g.ingredients);}d.dishLinks??=[];return {header:match[0],data:d};}
// EN: Exports authoritative ingredient groups, special-customer serving defaults and a shared queue while preserving legacy data.
// VI: Xuất nhóm nguyên liệu chính, thời gian mặc định của khách đặc biệt và hàng chung, giữ dữ liệu cũ.
function exportAsset(level,catalog=orderCatalog){const d=clone(level.data);if(d.useUnifiedCustomerQueue||d.customerQueue?.length){d.useUnifiedCustomerQueue=true;for(const c of d.customerQueue)if(c.useIngredientOrders||c.ingredientOrders?.length||c._ingredientDrafts)materializeOrder(c,catalog);}for(const q of [d.leftQueue||[],d.rightQueue||[],...(d.customerQueue?[d.customerQueue]:[])])for(const c of q){if([1,2].includes(c.customerType))c.timeServe??=30;if(c._ingredientDrafts)materializeOrder(c,catalog);c.finalFoods=pack(c.finalFoods||[]);if(c.ingredientOrders)for(const g of c.ingredientOrders)g.ingredients=pack(unpack(g.ingredients));delete c._ingredientDrafts;}return level.header+jsyaml.dump({MonoBehaviour:d},{lineWidth:-1,noRefs:true,flowLevel:-1,forceQuotes:false});}
// EN: Returns one ordered queue, interleaving legacy left/right entries without losing customers.
// VI: Trả một hàng khách có thứ tự, xen kẽ hàng trái/phải cũ và không làm mất khách.
function customerQueue(d){if(d.useUnifiedCustomerQueue||d.customerQueue?.length)return d.customerQueue||[];const result=[];for(let i=0;i<Math.max(d.leftQueue.length,d.rightQueue.length);i++){if(d.leftQueue[i])result.push(d.leftQueue[i]);if(d.rightQueue[i])result.push(d.rightQueue[i]);}return result;}
// EN: Creates one empty single-dish cell with Unity defaults.
// VI: Tạo một ô đĩa đơn trống với mặc định Unity.
function emptyCell(){return {foodType:0,requiredCount:1,isHidden:0,isFrozen:0,blockValue:1,dishShape:0,secondFoodType:0,secondCount:1};}
// EN: Resolves a board cell using zero-based column and row coordinates.
// VI: Lấy ô board theo cột và hàng bắt đầu từ 0.
function cell(d,x,y){return d.ingredientBoard[x]?.foods?.[y];}
// EN: Finds the double-dish anchor covering a cell, matching Unity's adjacency rules.
// VI: Tìm ô gốc đĩa đôi che một ô, theo quy tắc kề của Unity.
function covering(d,x,y){if(y>0&&cell(d,x,y-1)?.dishShape===1)return [x,y-1];if(x>0&&cell(d,x-1,y)?.dishShape===2)return [x-1,y];return null;}
// EN: Validates customer types and positive serving times, direct ingredient requirements and board compatibility without requiring named recipes.
// VI: Kiểm tra loại khách và thời gian phục vụ dương, nguyên liệu trực tiếp và board, không yêu cầu công thức có tên.
function analyze(d,catalog){const issues=[],need={},have={},known=new Set(catalog.ingredients.map(i=>i.id));let customers=0,dishes=0,slots=0;for(const c of customerQueue(d)){customers++;if(![0,1,2].includes(c.customerType??0))issues.push('Loại khách không hợp lệ.');if([1,2].includes(c.customerType)&&(!Number.isFinite(c.timeServe??30)||(c.timeServe??30)<=0))issues.push('Thời gian phục vụ phải lớn hơn 0 giây.');const groups=orderIngredients(c,catalog);if(!groups.length||groups.length>4)issues.push('Mỗi khách cần từ 1 đến 4 nhóm nguyên liệu.');for(const items of groups){dishes++;if(!items.length)issues.push('Nhóm chưa chọn nguyên liệu.');if(items.length>6)issues.push('Mỗi nhóm hỗ trợ tối đa 6 nguyên liệu.');for(const id of items){if(!known.has(id))issues.push('Nguyên liệu order không tồn tại: '+id);need[id]=(need[id]||0)+1;}}}
if(d.ingredientBoard.length!==d.boardColumns)issues.push('Số cột dữ liệu không khớp boardColumns.');for(let x=0;x<d.boardColumns;x++){if(d.ingredientBoard[x]?.foods?.length!==d.boardRows)issues.push('Cột '+(x+1)+' không khớp boardRows.');for(let y=0;y<d.boardRows;y++){const c=cell(d,x,y);if(!c)continue;const label=`C${x+1} H${y+1}`;if(covering(d,x,y)){if(c.foodType||c.dishShape)issues.push(label+': ô bị đĩa đôi che phải để trống.');continue;}if(c.foodType){slots++;if(!known.has(c.foodType))issues.push(label+': nguyên liệu không tồn tại.');if(!Number.isInteger(c.requiredCount)||c.requiredCount<1)issues.push(label+': số phần phải là số nguyên dương.');have[c.foodType]=(have[c.foodType]||0)+c.requiredCount;}if(c.isFrozen&&c.blockValue<1)issues.push(label+': độ bền băng phải dương.');if(c.dishShape){if(![1,2].includes(c.dishShape))issues.push(label+': kiểu đĩa không hợp lệ.');const nx=x+(c.dishShape===2?1:0),ny=y+(c.dishShape===1?1:0);if(!cell(d,nx,ny))issues.push(label+': đĩa đôi tràn board.');if(!c.foodType||!c.secondFoodType||!known.has(c.secondFoodType)||!Number.isInteger(c.secondCount)||c.secondCount<1)issues.push(label+': đĩa đôi thiếu nguyên liệu hoặc số lượng hợp lệ.');if(c.secondFoodType)have[c.secondFoodType]=(have[c.secondFoodType]||0)+c.secondCount;}}}
const seen=new Set();for(const [index,link] of (d.dishLinks||[]).entries()){if(!Array.isArray(link.dishes)||link.dishes.length<2||link.dishes.length>3){issues.push('Link '+(index+1)+': cần 2–3 ô.');continue;}for(const p of link.dishes){const key=p.x+','+p.y;if(!cell(d,p.x,p.y)?.foodType||covering(d,p.x,p.y))issues.push('Link '+(index+1)+': ô không hợp lệ.');if(seen.has(key))issues.push('Link '+(index+1)+': ô bị lặp hoặc thuộc nhiều nhóm.');seen.add(key);}}
const ids=[...new Set([...Object.keys(need),...Object.keys(have)])].map(Number);const balance=ids.map(id=>({id,need:need[id]||0,have:have[id]||0,delta:(have[id]||0)-(need[id]||0)}));return {issues,balance,customers,dishes,slots,totalNeed:Object.values(need).reduce((a,b)=>a+b,0),totalHave:Object.values(have).reduce((a,b)=>a+b,0)};}
globalThis.Core={setCatalog,orderIngredients,matchingRecipe,setOrderGroups,materializeOrder,materializeLevel,customerQueue,clone,unpack,pack,parseAsset,exportAsset,emptyCell,cell,covering,analyze};
