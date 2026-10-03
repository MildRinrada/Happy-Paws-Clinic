/** The customer register's list page. */
export const customerList = {
  metaTitle: 'ลูกค้าและสัตว์ · Happy Paws Clinic',
  title: 'ลูกค้าและสัตว์',
  subtitle: 'ค้นหาเจ้าของ หรือลงทะเบียนลูกค้าใหม่ที่เคาน์เตอร์',
  register: '+ ลงทะเบียนลูกค้าใหม่',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูทะเบียนลูกค้า (customer:read)',
  search: {
    label: 'ค้นหาลูกค้า',
    placeholder: 'ชื่อเจ้าของ เบอร์โทร ชื่อสัตว์ หรือเลข HN',
    submit: 'ค้นหา',
    clear: 'ล้าง',
  },
  recentHeading: 'ลงทะเบียนล่าสุด',
  resultsHeading: (q: string) => `ผลการค้นหา "${q}"`,
  columns: { name: 'ชื่อ', phone: 'เบอร์โทร', email: 'อีเมล', portal: 'บัญชีออนไลน์' },
  portal: { yes: 'ใช้งานแล้ว', no: 'ยังไม่มี' },
  inactive: 'ปิดใช้งาน',
  empty: {
    title: 'ไม่พบลูกค้า',
    search: 'ลองค้นด้วยเบอร์โทร หรือชื่อสัตว์เลี้ยงแทน หรือลงทะเบียนเป็นลูกค้าใหม่',
    none: 'ยังไม่มีลูกค้าในระบบ เริ่มจากลงทะเบียนลูกค้าคนแรก',
  },
  next: 'หน้าถัดไป',
};
