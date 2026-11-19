const COLS=4,ROWS=4,CELL=144,N=16;
const origin={x:188,y:435},wallet={x:94,y:42};
const count=10,scatterTime=0.17,dropTime=0.14,dropDistance=13,launchGap=0.085;
let fps=25,coins=[],start=0;
const fpsIn=document.getElementById('fps');
// EN: Updates the single-coin preview rate; reward flips follow flight progress.
// VI: Cập nhật tốc độ xu xem thử riêng; xu thưởng lật theo tiến độ bay.
fpsIn.oninput=()=>{fps=+fpsIn.value;document.getElementById('fpsv').textContent=fps};
const one=document.getElementById('one').getContext('2d');
const flow=document.getElementById('flow').getContext('2d');
// EN: Draws one wrapped sheet frame at the requested centre and size.
// VI: Vẽ một khung trong sheet theo vòng lặp tại tâm và kích thước yêu cầu.
function draw(ctx,f,x,y,size){
 f=((Math.floor(f)%N)+N)%N;
 ctx.drawImage(img,(f%COLS)*CELL,Math.floor(f/COLS)*CELL,CELL,CELL,x-size/2,y-size/2,size,size);
}
// EN: Creates a compact burst with separate launch delays, curved paths and flip phases.
// VI: Tạo cụm xu bung nhỏ với thời điểm bay, đường cong và pha lật riêng.
function burst(){
 start=performance.now();coins=[];
 for(let i=0;i<count;i++){
  const angle=(i/count)*Math.PI*2+Math.random()*0.4;
  const radius=12+Math.random()*18;
  const x=origin.x+Math.cos(angle)*radius,y=origin.y+Math.sin(angle)*radius;
  coins.push({x,y,delay:i*launchGap,dur:0.76+Math.random()*0.08,
   bend:(i%2===0?-1:1)*(12+Math.random()*19),phase:Math.random()*N});
 }
}
document.getElementById('again').onclick=burst;
// EN: Samples an outward pop, a short downward settle and an accelerating staggered flight.
// VI: Tính vị trí xu bung, rơi nhẹ rồi tăng tốc bay lệch nhịp theo đường cong bậc hai.
function sampleCoin(c,elapsed){
 if(elapsed<0)return null;
 if(elapsed<scatterTime){
  const p=elapsed/scatterTime,e=1-Math.pow(1-p,3);
  return {x:origin.x+(c.x-origin.x)*e,y:origin.y+(c.y-origin.y)*e,size:38*(0.45+0.55*Math.min(1,p*2.5)),age:elapsed};
 }
 const age=elapsed-scatterTime-c.delay;
 if(age<0)return {x:c.x,y:c.y,size:38,age:elapsed};
 if(age<dropTime)return {x:c.x,y:c.y+dropDistance*Math.pow(age/dropTime,2),size:38,age:elapsed};
 const p=(age-dropTime)/c.dur;if(p>=1)return null;
 const u=Math.pow(p,1.65),v=1-u;
 const fromY=c.y+dropDistance;
 const cx=c.x+(wallet.x-c.x)*0.22+c.bend,cy=fromY-110;
 return {x:v*v*c.x+2*v*u*cx+u*u*wallet.x,y:v*v*fromY+2*v*u*cy+u*u*wallet.y,
  size:38-9*Math.pow(p,3),age:elapsed};
}
// EN: Draws the wallet with a short scale pulse and warm flash for each arrived coin.
// VI: Vẽ ví với nhịp phóng nhẹ và lóe sáng vàng khi mỗi xu đến.
function drawWallet(elapsed){
 const landed=coins.filter(c=>elapsed>=scatterTime+dropTime+c.delay+c.dur).length;
 let pulse=0;
 for(const c of coins){const age=elapsed-scatterTime-dropTime-c.delay-c.dur;if(age>=0&&age<0.15)pulse=Math.max(pulse,Math.sin(age/0.15*Math.PI));}
 flow.fillStyle='#273b60';flow.fillRect(65,19,154,46);
 if(pulse>0){
  const glow=flow.createRadialGradient(wallet.x,wallet.y,4,wallet.x,wallet.y,31);
  glow.addColorStop(0,`rgba(255,246,156,${pulse*0.95})`);glow.addColorStop(1,'rgba(255,213,65,0)');
  flow.fillStyle=glow;flow.beginPath();flow.arc(wallet.x,wallet.y,31,0,Math.PI*2);flow.fill();
 }
 draw(flow,0,wallet.x,wallet.y,32*(1+0.15*pulse));
 flow.fillStyle='#fff';flow.font='bold 20px sans-serif';flow.fillText(String(1000+landed*10),120,49);
}
// EN: Converges every random flip phase to the frontal frame during the final tenth of its journey.
// VI: Đưa mọi pha lật ngẫu nhiên về khung chính diện trong một phần mười cuối chuyến bay.
function flightFrame(c,elapsed){
 const lifetime=scatterTime+dropTime+c.delay+c.dur;
 const progress=Math.min(1,Math.max(0,elapsed/lifetime)/0.9);
 if(elapsed>=lifetime*0.9)return 0;
 const eased=1-Math.pow(1-progress,1.2);
 return c.phase+(32-c.phase)*eased;
}
// EN: Renders staggered reward coins with progress-synchronized flips before wallet impacts.
// VI: Vẽ xu thưởng nối đuôi và lật theo tiến độ bay trước phản hồi chạm ví.
function renderFlight(elapsed){
 flow.clearRect(0,0,320,520);
 flow.fillStyle='rgba(38,69,87,0.2)';flow.beginPath();flow.ellipse(origin.x,origin.y+21,35,8,0,0,Math.PI*2);flow.fill();
 if(elapsed<0.48){flow.save();flow.globalAlpha=Math.max(0,1-Math.max(0,elapsed-0.25)/0.23);flow.fillStyle='#fff8cb';flow.font='bold 23px sans-serif';flow.textAlign='center';flow.fillText('+100',origin.x,origin.y-35-elapsed*20);flow.restore();}
 for(const c of coins){const pose=sampleCoin(c,elapsed);if(pose)draw(flow,flightFrame(c,elapsed),pose.x,pose.y,pose.size);}
 drawWallet(elapsed);
}
// EN: Animates the single-coin preview and reward flight using elapsed time.
// VI: Chạy xem thử xu lật và hiệu ứng nhận thưởng theo thời gian thực.
function loop(t){
 one.clearRect(0,0,220,220);draw(one,t/1000*fps,110,110,180);
 renderFlight((t-start)/1000);requestAnimationFrame(loop);
}
// EN: Displays the sheet and starts a repeating flight preview after the embedded sprite loads.
// VI: Hiển thị sheet và bắt đầu xem thử chuyến bay lặp lại sau khi sprite nhúng tải xong.
img.onload=()=>{
 document.getElementById('sheet').getContext('2d').drawImage(img,0,0);
 burst();setInterval(burst,3400);requestAnimationFrame(loop);
};
