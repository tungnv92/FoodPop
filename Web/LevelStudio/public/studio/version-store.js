const LevelVersions={connection:null};
// EN: Opens a separate IndexedDB database for persistent per-level snapshots.
// VI: Mở cơ sở dữ liệu IndexedDB riêng để lưu phiên bản lâu dài cho từng level.
LevelVersions.open=function(){if(this.connection)return this.connection;this.connection=new Promise((resolve,reject)=>{const request=indexedDB.open('foodpop-level-versions',1);request.onupgradeneeded=()=>{const store=request.result.createObjectStore('versions',{keyPath:'id'});store.createIndex('levelId','levelId');};request.onsuccess=()=>resolve(request.result);request.onerror=()=>reject(request.error);request.onblocked=()=>reject(Error('Đóng tab FoodPop cũ rồi thử lại.'));});this.connection.catch(()=>this.connection=null);return this.connection;};
// EN: Assigns unique local identities, separating duplicates while keeping renamed levels attached to their history.
// VI: Gán định danh local riêng, tách level nhân bản và giữ lịch sử khi đổi tên level.
LevelVersions.identify=function(levels){const seen=new Set();for(const level of levels){if(typeof level._studioId!=='string'||seen.has(level._studioId))level._studioId=crypto.randomUUID();seen.add(level._studioId);}};
// EN: Commits an immutable snapshot and resolves only after the database transaction completes.
// VI: Lưu bản chụp độc lập và chỉ xác nhận sau khi giao dịch cơ sở dữ liệu hoàn tất.
LevelVersions.capture=async function(level,note='Lưu thủ công'){const snapshot=Core.clone(level);if(!snapshot._studioId)throw Error('Level chưa có định danh lịch sử.');const record={id:crypto.randomUUID(),levelId:snapshot._studioId,savedAt:new Date().toISOString(),note,snapshot};const db=await this.open();await new Promise((resolve,reject)=>{const tx=db.transaction('versions','readwrite');tx.objectStore('versions').add(record);tx.oncomplete=resolve;tx.onerror=()=>reject(tx.error);tx.onabort=()=>reject(tx.error||Error('Không lưu được phiên bản.'));});return record;};
// EN: Retrieves snapshots for exactly one local level identity, newest first.
// VI: Lấy phiên bản của đúng một định danh level local, bản mới nhất đứng trước.
LevelVersions.list=async function(levelId){const db=await this.open();return new Promise((resolve,reject)=>{const tx=db.transaction('versions','readonly'),request=tx.objectStore('versions').index('levelId').getAll(levelId);request.onsuccess=()=>resolve(request.result.sort((a,b)=>b.savedAt.localeCompare(a.savedAt)));request.onerror=()=>reject(request.error);});};
