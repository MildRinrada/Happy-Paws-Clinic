/** The stock list: "ยาและเวชภัณฑ์, คงเหลือรายสาขา". */
export const itemsPage = {
  metaTitle: 'สต๊อกยา · Happy Paws Clinic',
  title: 'สต๊อกยา',
  subtitle: 'ยาและเวชภัณฑ์ของสาขา จำนวนที่ใช้ได้ไม่นับล็อตที่หมดอายุ',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูสต๊อก (inventory:read)',
  empty: 'ยังไม่มีรายการในคลัง รายการใหม่เกิดจากการรับเข้าหรือการสั่งซื้อ',
  noMatch: 'ไม่พบรายการที่ตรงกับตัวกรอง',
  count: (shown: number) => `${shown} รายการ`,

  filters: {
    label: 'ค้นหาและกรองรายการ',
    search: 'ค้นหาชื่อ',
    searchPlaceholder: 'เช่น Amoxicillin',
    status: 'สถานะ',
    any: 'ทั้งหมด',
    apply: 'ค้นหา',
    clear: 'ล้าง',
  },

  columns: {
    name: 'รายการ',
    price: 'ราคาขาย/หน่วย',
    available: 'ใช้ได้',
    status: 'สถานะ',
    onOrder: 'สั่งไว้',
    expiry: 'หมดอายุก่อน',
    lots: 'ล็อต',
  },

  perUnit: (unit: string) => `ต่อ${unit}`,
  onHand: (count: number) => `ในคลัง ${count} (รวมที่หมดอายุ)`,
  meta: (kind: string, reorderLevel: number) => `${kind}, จุดสั่งซื้อ ${reorderLevel}`,
  lot: (number: string | undefined, quantity: number) => `${number ?? 'ไม่มีเลขล็อต'}: ${quantity}`,
  expiredLot: 'หมดอายุ',
  none: '-',
};
