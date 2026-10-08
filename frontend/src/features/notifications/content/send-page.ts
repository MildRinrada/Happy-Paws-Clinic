import type { SendChannel } from '../types/notification.types';

export interface SendableTemplate {
  key: string;
  label: string;
  variables: { name: string; required: boolean; maxLength: number; multiline?: boolean }[];
}

/**
 * The templates a member of staff may send by hand, with the values each needs. A copy of the
 * backend's catalogue (which checks them again), because reading the catalogue needs
 * `notify:manage` and a sender may hold only `notify:send`.
 */
export const sendableTemplates: SendableTemplate[] = [
  {
    key: 'appointment.reminder',
    label: 'เตือนนัดหมาย',
    variables: [
      { name: 'pet_name', required: true, maxLength: 100 },
      { name: 'date', required: true, maxLength: 40 },
      { name: 'time', required: true, maxLength: 20 },
      { name: 'vet_name', required: false, maxLength: 100 },
    ],
  },
  {
    key: 'vaccine.due',
    label: 'ครบกำหนดวัคซีน',
    variables: [
      { name: 'pet_name', required: true, maxLength: 100 },
      { name: 'date', required: true, maxLength: 40 },
      { name: 'vaccine_name', required: false, maxLength: 100 },
    ],
  },
  {
    key: 'clinic.message',
    label: 'ข้อความถึงเจ้าของ',
    variables: [{ name: 'message', required: true, maxLength: 500, multiline: true }],
  },
];

/** What each value is called on the form, and an example of what to type. */
export const variableLabels: Record<string, { label: string; placeholder?: string }> = {
  pet_name: { label: 'ชื่อสัตว์เลี้ยง', placeholder: 'ลูน่า' },
  vet_name: { label: 'ชื่อหมอ (ไม่บังคับ)', placeholder: 'หมอเอ' },
  date: { label: 'วันที่', placeholder: '12 ต.ค.' },
  time: { label: 'เวลา', placeholder: '10:30' },
  vaccine_name: { label: 'ชื่อวัคซีน (ไม่บังคับ)', placeholder: 'พิษสุนัขบ้า' },
  message: { label: 'ข้อความ' },
  number: { label: 'เลขคิว' },
  room: { label: 'ห้องตรวจ' },
  receipt_number: { label: 'เลขที่ใบเสร็จ' },
  amount: { label: 'ยอดเงิน' },
  service_name: { label: 'ชื่อบริการ' },
  display_name: { label: 'ชื่อลูกค้า' },
  record_id: { label: 'รหัสเวชระเบียน' },
  invoice_id: { label: 'รหัสบิล' },
};

export const channelChoices: { value: SendChannel; label: string }[] = [
  { value: 'auto', label: 'อัตโนมัติ (LINE ก่อน แล้ว SMS หรืออีเมล)' },
  { value: 'line', label: 'LINE เท่านั้น' },
  { value: 'sms', label: 'SMS เท่านั้น' },
  { value: 'email', label: 'อีเมลเท่านั้น' },
];

/** The send page. */
export const sendPage = {
  metaTitle: 'ส่งข้อความถึงลูกค้า · Happy Paws Clinic',
  title: 'ส่งข้อความถึงลูกค้า',
  subtitle: 'ส่งได้ทีละคนหรือหลายคนพร้อมกัน ลูกค้าที่ไม่ได้ยินยอมรับข้อความจะถูกข้าม และบอกไว้ในผลการส่ง',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ส่งข้อความถึงลูกค้า',
  noCustomerSearch: 'บัญชีนี้ค้นหาลูกค้าไม่ได้ (ต้องมีสิทธิ์ customer:read) จึงเลือกผู้รับไม่ได้',
  heading: 'ข้อความ',
  template: 'ชนิดข้อความ',
  channel: 'ช่องทาง',
  channelHint: 'เลือกช่องทางเดียวแล้วลูกค้าที่ไม่มีช่องทางนั้นจะไม่ได้รับ ไม่ส่งทางอื่นแทน',
  recipients: 'ผู้รับ',
  noRecipients: 'ยังไม่ได้เลือกผู้รับ',
  remove: (name: string) => `เอา ${name} ออก`,
  submit: 'ส่ง',
  sending: 'กำลังส่ง…',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  maxRecipients: 100,
  tooMany: 'ส่งได้ไม่เกิน 100 คนต่อครั้ง',
  search: {
    label: 'ค้นหาลูกค้า',
    placeholder: 'ชื่อ เบอร์โทร HN หรือชื่อสัตว์',
    submit: 'ค้นหา',
    searching: 'กำลังค้นหา…',
    empty: 'ไม่พบลูกค้าที่ตรงกับคำค้น',
    add: 'เพิ่ม',
    added: 'เลือกแล้ว',
  },
};

/** The answer of a send or a broadcast. */
export const sendResultText = {
  heading: 'ผลการส่ง',
  summary: (sent: number, failed: number, skipped: number) => `ส่งแล้ว ${sent} คน ไม่สำเร็จ ${failed} คน ไม่ได้ส่ง ${skipped} คน`,
  viaFallback: (from: string) => `(${from} ไม่สำเร็จ จึงส่งทางนี้แทน)`,
  viewLog: 'ดูในประวัติการส่ง',
  quota: (remaining: number, limit: number) => `โควตาประกาศเดือนนี้เหลือ ${remaining} จาก ${limit} ข้อความ`,
};
