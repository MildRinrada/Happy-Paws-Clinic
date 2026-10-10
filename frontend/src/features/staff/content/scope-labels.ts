/**
 * Thai names for the scope catalogue's resources and actions, so the role editor reads as what a
 * clinic does rather than as `resource:action`. The code itself is always shown beside the name:
 * it is what the docs and error messages use. Anything not listed falls back to its code.
 */

const resources: Record<string, string> = {
  analytics: 'วิเคราะห์ข้อมูล',
  appointment: 'นัดหมาย',
  audit: 'บันทึกการใช้งาน (audit log)',
  automation: 'งานอัตโนมัติ',
  billing: 'การเงินของคลินิก',
  consult: 'การตรวจ',
  content: 'บทความบนเว็บไซต์',
  crm: 'ลูกค้าสัมพันธ์',
  customer: 'ลูกค้า',
  dashboard: 'แดชบอร์ด',
  integration: 'การเชื่อมต่อระบบภายนอก',
  invoice: 'ใบแจ้งหนี้',
  inventory: 'คลังสินค้าและยา',
  lab: 'แล็บ',
  message: 'ข้อความ',
  notify: 'การแจ้งเตือน',
  patient: 'สัตว์เลี้ยง',
  payment: 'การรับเงิน',
  queue: 'คิว',
  receipt: 'ใบเสร็จ',
  record: 'เวชระเบียน',
  report: 'รายงาน',
  role: 'role และสิทธิ์',
  rx: 'ใบสั่งยา',
  schedule: 'ตารางเวร',
  service: 'บริการและราคา',
  settings: 'การตั้งค่า',
  staff: 'พนักงาน',
  stats: 'สถิติ',
  user: 'บัญชีผู้ใช้',
  vitals: 'สัญญาณชีพ',
};

const actions: Record<string, string> = {
  read: 'ดู',
  write: 'เพิ่มและแก้ไข',
  manage: 'จัดการ',
  danger: 'การตั้งค่าที่ย้อนกลับไม่ได้',
  merge: 'รวมรายชื่อซ้ำ',
  sign: 'เซ็นรับรอง',
  amend: 'แก้ไขหลังเซ็น',
  audit: 'ดูประวัติการแก้ไข',
  checkin: 'เช็กอิน',
  call: 'เรียกคิว',
  dispense: 'จ่ายยา',
  request: 'ส่งตรวจ',
  send: 'ส่ง',
  broadcast: 'ส่งถึงทุกคน',
  hr: 'ข้อมูลบุคลากร',
  discount: 'ให้ส่วนลด',
  collect: 'รับชำระ',
  refund: 'คืนเงิน',
  payout: 'จ่ายเงินออก',
  issue: 'ออกเอกสาร',
  adjust: 'ปรับยอด',
  purchase: 'สั่งซื้อ',
  export: 'ส่งออก',
};

export const resourceLabel = (resource: string) => resources[resource] ?? resource;
export const actionLabel = (action: string) => actions[action] ?? action;
