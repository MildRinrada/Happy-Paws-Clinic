/** `/crm/segments`, the saved groups, and `/crm/segments/{id}`, one group counted again. */
export const segmentListText = {
  heading: 'กลุ่มที่บันทึกไว้',
  hint: 'ตัวเลขในรายการคือจำนวนตอนบันทึก เปิดกลุ่มเพื่อนับใหม่',
  empty: 'ยังไม่มีกลุ่มลูกค้า',
  noRead: 'บัญชีนี้ดูรายการกลุ่มไม่ได้ (ต้องมีสิทธิ์ crm:read)',
  counts: (customers: number, reachable: number) =>
    `${customers.toLocaleString('th-TH')} คน ส่ง LINE ถึง ${reachable.toLocaleString('th-TH')} คน`,
  savedAt: (when: string) => `บันทึกเมื่อ ${when}`,
  open: 'เปิดดู',
  next: 'หน้าถัดไป',
  first: 'กลับหน้าแรก',

  detail: {
    back: 'กลับไปกลุ่มลูกค้า',
    countedAt: (when: string) => `นับเมื่อ ${when}`,
    criteria: 'เงื่อนไข',
    notFound: 'ไม่พบกลุ่มนี้',
    refused: 'บัญชีนี้ดูกลุ่มนี้ไม่ได้ กลุ่มของสาขาอื่นหรือกลุ่มทุกสาขาดูได้เฉพาะบัญชีที่ไม่ผูกสาขา',
  },

  criteria: {
    species: (list: string) => `ชนิดสัตว์ ${list}`,
    inactive: (days: number) => `ไม่มา ${days.toLocaleString('th-TH')} วัน`,
    within: (days: number) => `มาภายใน ${days.toLocaleString('th-TH')} วัน`,
    spend: (amount: string) => `ใช้จ่ายใน 365 วันอย่างน้อย ${amount}`,
    tiers: (list: string) => `ระดับ ${list}`,
    package: (has: boolean) => (has ? 'มีแพ็กเกจที่ใช้ได้' : 'ไม่มีแพ็กเกจที่ใช้ได้'),
    branch: 'เฉพาะลูกค้าที่เคยมาสาขานี้',
  },
};
