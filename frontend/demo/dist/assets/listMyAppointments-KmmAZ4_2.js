import{s}from"./index-Ci3v_PYW.js";async function p(t,a=10){const{data:n}=await s().get("/portal/appointments",{query:{upcoming:t,limit:a}});return n}export{p as l};
