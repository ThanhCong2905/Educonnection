'use strict';
(() => {
 const panel=document.querySelector('[data-chat-panel]'),form=document.getElementById('chat-form'),stream=document.getElementById('chat-stream');
 if(!panel?.dataset.selected || !form || !stream)return;
 const feedback=document.getElementById('chat-feedback'),input=document.getElementById('chat-text'),button=form.querySelector('button');
 let busy=false,reading=false,stopped=false;
 const render=messages=>{
  const nearBottom=stream.scrollHeight-stream.scrollTop-stream.clientHeight<80;
  const ids=[...stream.querySelectorAll('[data-message-id]')].map(x=>x.dataset.messageId);
  if(ids.join(',')===messages.map(x=>x.id).join(','))return;
  const nodes=messages.map(message=>{
   const article=document.createElement('article');article.className='chat-message'+(message.mine?' mine':'');article.dataset.messageId=message.id;
   const text=document.createElement('p');text.textContent=message.text;
   const time=document.createElement('time');time.dateTime=message.time;time.textContent=new Date(message.time).toLocaleString('vi-VN',{day:'2-digit',month:'2-digit',hour:'2-digit',minute:'2-digit'});
   article.append(text,time);return article;
  });stream.replaceChildren(...nodes);if(nearBottom)stream.scrollTop=stream.scrollHeight;
 };
 const refresh=async()=>{
  if(reading||stopped||document.hidden||panel.dataset.page!=='1')return;
  reading=true;
  try {
   const response=await fetch(panel.dataset.feed,{headers:{Accept:'application/json','X-Background-Request':'true'},cache:'no-store'});
   if(response.status===401||response.status===403){stopped=true;feedback.textContent='Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';return;}
   if(!response.ok)throw new Error();render(await response.json());if(!busy)feedback.textContent='';
  }catch{if(!busy)feedback.textContent='Chưa cập nhật được tin mới. Hệ thống sẽ thử lại.';}finally{reading=false;}
 };
 form.addEventListener('submit',async event=>{
  event.preventDefault();if(busy||!form.reportValidity())return;
  busy=true;button.disabled=true;feedback.textContent='Đang gửi…';
  try{
   const response=await fetch(form.action,{method:'POST',body:new FormData(form),headers:{Accept:'application/json'}});
   const result=await response.json().catch(()=>({error:'Không gửi được tin nhắn. Vui lòng thử lại.'}));
   if(!response.ok)throw new Error(result.error||'Vui lòng đăng nhập lại hoặc thử lại.');
   input.value='';form.elements.OperationId.value=crypto.randomUUID();feedback.textContent='Đã gửi';
   if(panel.dataset.page!=='1'){location.href=location.pathname+'?id='+encodeURIComponent(panel.dataset.selected);return;}
   await refresh();stream.scrollTop=stream.scrollHeight;
  }catch(error){feedback.textContent=error.message;}finally{busy=false;button.disabled=false;}
 });
 // Keep the same operation ID when retrying a request whose response was lost.
 stream.scrollTop=stream.scrollHeight;refresh();const timer=setInterval(refresh,3000);
 addEventListener('pagehide',()=>{stopped=true;clearInterval(timer);});
})();
