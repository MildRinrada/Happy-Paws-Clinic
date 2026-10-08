/** The delivery log page. */
export const deliveryLogPage = {
  metaTitle: 'ประวัติการส่งแจ้งเตือน · Happy Paws Clinic',
  title: 'ประวัติการส่งแจ้งเตือน',
  subtitle: 'ทุกครั้งที่ระบบพยายามส่งข้อความถึงลูกค้า แยกตามช่องทาง ถ้า LINE ส่งไม่ได้แล้วส่ง SMS แทน จะเห็นเป็นสองแถว',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูประวัติการส่ง',
  empty: 'ยังไม่มีประวัติการส่งที่ตรงกับตัวกรอง',
  next: 'หน้าถัดไป',
  columns: {
    time: 'เวลา',
    customer: 'ลูกค้า',
    template: 'ข้อความ',
    channel: 'ช่องทาง',
    status: 'ผล',
  },
  fallbackFrom: (channel: string) => `ส่งแทน ${channel} ที่ไม่สำเร็จ`,
  filters: {
    label: 'กรองประวัติการส่ง',
    status: 'ผล',
    channel: 'ช่องทาง',
    source: 'ที่มา',
    all: 'ทั้งหมด',
    apply: 'กรอง',
    clear: 'ล้างตัวกรอง',
    batch: (id: string) => `เฉพาะการส่งชุด ${id}`,
  },
};
