/** The roles card on the staff page: each role's scopes, a new role, and rewriting one. */
export const roleSettings = {
  title: 'Role และสิทธิ์',
  intro: 'สิทธิ์ในระบบเป็น scope แบบ resource:action รวมเป็นชุดใน role แล้วมอบ role ให้พนักงาน การแก้ role มีผลกับทุกคนที่ถือ role นั้นภายใน 15 นาที',
  noAccess: 'บัญชีนี้ดู role ไม่ได้ (role:read)',
  catalogueUnavailable: 'โหลดรายการ scope ไม่ได้ จึงแก้ role จากหน้านี้ไม่ได้',
  count: (count: number) => `${count} role`,

  badges: {
    system: 'role ตั้งต้น',
    custom: 'สร้างเอง',
    portal: 'สำหรับลูกค้า แก้ไม่ได้',
    empty: 'ไม่มีสิทธิ์ (เลิกใช้)',
  },

  summary: {
    everything: 'ทุกสิทธิ์ (*)',
    scopes: (count: number) => `${count} scope`,
    showScopes: (count: number) => `ดูทั้ง ${count} scope`,
    members: 'ดูพนักงานใน role นี้',
  },

  actions: {
    edit: 'แก้สิทธิ์',
    add: '+ สร้าง role',
  },

  locked: 'role นี้มีสิทธิ์ที่บัญชีของคุณไม่มี จึงแก้ไม่ได้ ระบบไม่ให้ใครให้หรือเอาสิทธิ์ที่ตัวเองไม่มีออก',

  form: {
    name: 'ชื่อ role',
    nameHint: '2 ถึง 64 ตัวอักษร ไม่ขึ้นต้นหรือลงท้ายด้วยช่องว่าง แนะนำภาษาอังกฤษพิมพ์เล็กกับ _ เช่น senior_nurse เพราะชื่อนี้คือรหัสที่ระบบใช้ และเปลี่ยนภายหลังไม่ได้',
    scopes: 'สิทธิ์ใน role นี้',
    scopesHint: 'ช่องที่เลือกไม่ได้คือสิทธิ์ที่บัญชีของคุณไม่มี',
    emptyWarning: 'ไม่ได้เลือกสิทธิ์เลย คนที่ถือ role นี้จะทำอะไรไม่ได้ ใช้เมื่อต้องการเลิกใช้ role (ระบบยังไม่มีการลบ role)',
    lockoutNote: 'ถ้าการแก้นี้ทำให้ไม่เหลือใครที่มีทุกสิทธิ์ (*) ระบบจะไม่บันทึก',
  },

  picker: {
    everything: 'ทุกสิทธิ์ (*)',
    everythingHint: 'รวม scope ที่จะเพิ่มในอนาคตด้วย ให้เฉพาะเจ้าของคลินิก',
    wholeResource: 'ทุกอย่างในหมวดนี้',
    wholeResourceHint: (scope: string) => `${scope} รวม action ที่จะเพิ่มในอนาคต`,
    coveredByEverything: 'เลือกทุกสิทธิ์ (*) ไว้แล้ว',
    selected: (count: number) => `เลือก ${count} รายการ`,
  },
};
