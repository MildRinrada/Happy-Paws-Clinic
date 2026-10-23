function a(n,o){return n.branches.find(r=>r.branch_id===o)??n.branches[0]??{branch_id:o??"",on_hand:0,available:0,status:"out",on_order:0,lots:[],open_orders:[]}}export{a as s};
