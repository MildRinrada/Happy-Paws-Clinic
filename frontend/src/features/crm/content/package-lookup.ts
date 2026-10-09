/** `/crm/customer-packages`: one customer's packages, looked up by search. */
export const packageLookupText = {
  title: 'แพ็กเกจของลูกค้า',
  subtitle: 'ค้นหาลูกค้าเพื่อดูแพ็กเกจทั้งหมด สิทธิ์ที่เหลือ และวันหมดอายุ ทุกสาขา',
  heading: (name: string) => `แพ็กเกจของ ${name}`,
  empty: 'ไม่มีแพ็กเกจตามตัวกรองนี้',
  notFound: 'ไม่พบลูกค้ารายนี้',
  status: 'สถานะ',
  any: 'ทั้งหมด',
  apply: 'กรอง',
  filterLabel: 'กรองแพ็กเกจ',
};
