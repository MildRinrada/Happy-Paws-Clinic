async function t(e,r){try{return(await fetch(e.url,{method:"PUT",headers:e.headers,body:r})).ok}catch{return!1}}export{t as u};
