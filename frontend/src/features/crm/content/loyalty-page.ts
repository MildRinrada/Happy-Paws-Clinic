/** `/crm/loyalty`: one customer's points, membership level, coupons used and packages. */
export const loyaltyPage = {
  title: 'แต้มและสมาชิก',
  subtitle: 'ค้นหาลูกค้าเพื่อดูแต้มคงเหลือ ระดับสมาชิก ประวัติแต้ม คูปองที่ใช้ และแพ็กเกจ',

  lookup: {
    label: 'ค้นหาลูกค้า',
    placeholder: 'ชื่อ เบอร์โทร HN หรือชื่อสัตว์',
    submit: 'ค้นหา',
    searching: 'กำลังค้นหา…',
    empty: 'ไม่พบลูกค้าที่ตรงกับคำค้น',
    open: 'ดูแต้ม',
    idLabel: 'รหัสลูกค้า',
    idPlaceholder: 'cus_…',
    idSubmit: 'ดูแต้ม',
    noSearch: 'บัญชีนี้ค้นหาลูกค้าไม่ได้ (ต้องมีสิทธิ์ customer:read) กรอกรหัสลูกค้าแทน',
  },

  notFound: 'ไม่พบลูกค้ารายนี้',
  customerLink: 'เปิดหน้าลูกค้า',

  summary: {
    balance: 'แต้มคงเหลือ',
    points: (count: number) => `${count.toLocaleString('th-TH')} แต้ม`,
    worth: (amount: string) => `แลกเป็นส่วนลดได้ ${amount}`,
    lifetime: (count: number) => `สะสมทั้งหมด ${count.toLocaleString('th-TH')} แต้ม`,
    tier: 'ระดับสมาชิก',
    toNext: (points: number, tier: string) => `อีก ${points.toLocaleString('th-TH')} แต้มถึงระดับ${tier}`,
    top: 'ระดับสูงสุดแล้ว',
    rules: (perPoint: string, value: string) =>
      `ได้ 1 แต้มต่อยอดชำระ ${perPoint} แต้มเข้าเมื่อบิลชำระครบ 1 แต้มแลกเป็นส่วนลดได้ ${value}`,
  },

  history: {
    heading: 'ประวัติแต้ม',
    empty: 'ยังไม่มีการเคลื่อนไหวของแต้ม',
    when: 'วันที่',
    kind: 'รายการ',
    invoice: 'บิล',
    points: 'แต้ม',
    note: 'หมายเหตุ',
    signed: (points: number) => `${points > 0 ? '+' : ''}${points.toLocaleString('th-TH')}`,
    limit: 'แสดง 50 รายการล่าสุด',
  },

  coupons: {
    heading: 'คูปองที่ใช้',
    empty: 'ยังไม่เคยใช้คูปอง',
    row: (code: string, amount: string) => `${code} ลด ${amount}`,
    released: 'คืนสิทธิ์แล้ว',
  },

  packages: {
    heading: 'แพ็กเกจ',
    empty: 'ยังไม่มีแพ็กเกจ',
  },
};
