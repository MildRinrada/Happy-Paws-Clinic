/** The "ธุรกิจ" tab: revenue, services, retention and staff performance. */
export const businessReport = {
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูรายงานธุรกิจ (analytics:read หรือ analytics:hr)',

  revenue: {
    title: 'รายรับ',
    net: 'รับสุทธิ',
    collected: 'รับชำระ',
    refunded: 'คืนเงิน',
    invoiced: 'ยอดที่ออกบิล',
    invoicedHint: (count: string) => `จากบิล ${count} ใบที่ไม่ได้ยกเลิก`,
    basis: 'นับเงินในวันที่รับหรือคืนจริง ส่วนลดและบิลที่ยกเลิกไม่ถูกนับเป็นรายรับ',
    chartLabel: 'รายรับสุทธิในแต่ละช่วง',
    byMethod: 'แยกตามช่องทาง',
    byBranch: 'แยกตามสาขา',
    columns: { name: 'ช่องทาง', branch: 'สาขา', collected: 'รับชำระ', refunded: 'คืนเงิน', net: 'สุทธิ' },
    methods: { cash: 'เงินสด', card: 'บัตร', promptpay: 'PromptPay' } as Record<string, string>,
  },

  services: {
    title: 'บริการทำเงิน',
    note: 'นับจากบิลที่ออกในช่วงนี้ เงินที่บิลเก็บได้จริง (รับชำระหักคืนเงิน) แบ่งให้แต่ละรายการตามสัดส่วนราคา ค่ายาในบิลเดียวกัน (ขึ้นบิลเองเมื่อจ่ายยา) แสดงข้างบริการ ส่วนนี้ยังไม่ใช่กำไร เพราะรายงานยังไม่หักต้นทุนยา',
    columns: {
      name: 'บริการ',
      quantity: 'จำนวน',
      billed: 'ราคาเต็ม',
      revenue: 'รายรับ',
      medicine: 'ค่ายาในบิลเดียวกัน',
      afterMedicine: 'หลังหักยา',
    },
    empty: 'ยังไม่มีบริการในบิลช่วงนี้',
    byKind: 'แยกตามประเภทรายการ',
    kinds: { service: 'บริการ', treatment: 'หัตถการ', medicine: 'ยา', other: 'อื่น ๆ' } as Record<string, string>,
  },

  retention: {
    title: 'ลูกค้าใหม่และลูกค้าที่กลับมา',
    note: 'นับการมาเป็นการเปิดเคสตรวจหรือการออกบิลที่ไม่ได้ยกเลิก ลูกค้าใหม่คือเดือนแรกที่เคยมา',
    columns: { month: 'เดือน', active: 'ลูกค้าที่มา', new: 'ใหม่', returning: 'กลับมา' },
    cohortTitle: 'การกลับมาของลูกค้าใหม่แต่ละเดือน',
    cohortColumns: { cohort: 'เดือนที่มาครั้งแรก', customers: 'จำนวน' },
    monthOffset: (offset: number) => (offset === 0 ? 'เดือนแรก' : `+${offset} เดือน`),
  },

  staff: {
    title: 'ผลงานรายบุคคล',
    noAccess: 'ผลงานรายบุคคลต้องใช้สิทธิ์ analytics:hr',
    note: 'รายได้ของเคสคือเงินที่รับหักคืนจากบิลของเคสที่คนนั้นตรวจ ระบบยังไม่มีคะแนนรีวิวให้แสดง',
    columns: {
      name: 'ชื่อ',
      cases: 'เคสที่ปิด',
      averageCase: 'เฉลี่ยต่อเคส',
      treatments: 'หัตถการ',
      attributed: 'รายได้จากเคส',
      collected: 'รับเงินที่เคาน์เตอร์',
    },
    empty: 'ไม่มีพนักงานในสาขานี้',
  },
};
