/** The webhook pages. */
export const webhookSettingsPage = {
  metaTitle: 'Webhook · Happy Paws Clinic',
  title: 'Webhook',
  subtitle: 'ส่งเหตุการณ์ในคลินิกไปยังระบบอื่นทันทีที่เกิด เช่น ระบบบัญชีหรือแอปของคลินิก',

  form: {
    heading: 'สร้าง webhook',
    url: 'URL ปลายทาง',
    urlHint: 'ต้องขึ้นต้นด้วย https:// และเป็นเครื่องที่เข้าถึงจากอินเทอร์เน็ตได้ ระบบไม่ส่งไปยังเครือข่ายภายใน',
    description: 'คำอธิบาย (ไม่บังคับ)',
    events: 'เหตุการณ์ที่ต้องการ',
    eventsHint: 'แต่ละเหตุการณ์ส่งแค่รหัสอ้างอิง เวลา สถานะ และยอดเงิน ไม่มีข้อมูลการรักษาและข้อมูลติดต่อของลูกค้า',
    submit: 'สร้าง webhook',
    pending: 'กำลังสร้าง…',
  },

  created: {
    heading: 'สร้าง webhook แล้ว',
    secret: 'Signing secret',
    warning: 'คัดลอก secret ไปเก็บในระบบปลายทางตอนนี้ ระบบจะไม่แสดงค่านี้อีก ถ้าทำหาย สร้าง secret ใหม่ได้จากรายการ webhook',
    signing: 'ตรวจลายเซ็นด้วย HMAC-SHA256 ของ "timestamp.body" จาก header X-Webhook-Timestamp และเทียบกับ header X-Signature',
    id: 'รหัส webhook',
    log: 'ดูประวัติการส่ง',
  },

  lookup: {
    heading: 'เปิดประวัติด้วยรหัส webhook',
    hint: 'ใช้เปิดประวัติของ webhook ที่ลบไปแล้ว รหัสขึ้นต้นด้วย whk_ ดูได้จากบันทึกการใช้งาน',
    label: 'รหัส webhook',
    submit: 'ดูประวัติ',
  },
};

/** Thai names of the contract's sixteen events. */
export const webhookEventLabels: Record<string, string> = {
  'appointment.created': 'มีนัดใหม่',
  'appointment.rescheduled': 'เลื่อนนัด',
  'appointment.cancelled': 'ยกเลิกนัด',
  'appointment.no_show': 'ไม่มาตามนัด',
  'appointment.checked_in': 'เช็กอินแล้ว',
  'queue.ticket.called': 'เรียกคิว',
  'consultation.closed': 'ปิดเคสการตรวจ',
  'record.signed': 'ลงนามเวชระเบียน',
  'lab.result.ready': 'ผลแล็บพร้อม',
  'prescription.dispensed': 'จ่ายยาแล้ว',
  'invoice.issued': 'ออกใบแจ้งหนี้',
  'payment.succeeded': 'รับชำระเงิน',
  'payment.refunded': 'คืนเงิน',
  'inventory.low_stock': 'สต๊อกใกล้หมด',
  'customer.created': 'ลูกค้าใหม่',
  'pet.created': 'สัตว์เลี้ยงใหม่',
};

/** The delivery log page. */
export const deliveryLogPage = {
  metaTitle: 'ประวัติการส่ง webhook · Happy Paws Clinic',
  title: 'ประวัติการส่ง webhook',
  back: 'กลับไปหน้า webhook',
  empty: 'ยังไม่มีการส่ง',
  emptyHint: 'การส่งจะปรากฏที่นี่ภายในไม่กี่วินาทีหลังเกิดเหตุการณ์ที่ webhook นี้รับ',
  notFound: 'ไม่พบ webhook รหัสนี้',
  next: 'หน้าถัดไป',
  retryNote: 'ส่งไม่สำเร็จจะลองใหม่อีก 5 ครั้ง ห่างกัน 1, 5, 25, 125 และ 625 นาที',
  columns: {
    time: 'เวลา',
    event: 'เหตุการณ์',
    status: 'สถานะ',
    attempts: 'ครั้งที่ลอง',
    result: 'ผลครั้งล่าสุด',
    action: 'ส่งซ้ำ',
  },
  deletedNote: 'webhook นี้ถูกลบแล้ว ยังดูประวัติได้ แต่ส่งซ้ำไม่ได้',
  disabledNote: 'webhook นี้ปิดอยู่ เปิดใช้งานก่อนจึงจะส่งซ้ำได้',
  redeliver: 'ส่งอีกครั้ง',
  redelivering: 'กำลังส่ง…',
  redelivered: 'เข้าคิวแล้ว จะส่งภายในไม่กี่วินาที',
  attempts: (done: number, max: number) => (done > max ? `${done} ครั้ง (รวมส่งซ้ำ)` : `${done} จาก ${max}`),
  nextAttempt: (when: string) => `ลองอีกครั้ง ${when}`,
  answer: 'คำตอบจากปลายทาง (ตัดให้สั้น)',
};

export const deliveryStatusLabels: Record<string, string> = {
  pending: 'รอส่ง',
  succeeded: 'สำเร็จ',
  failed: 'ไม่สำเร็จ',
};

export const deliveryStatusTones: Record<string, 'amber' | 'green' | 'red'> = {
  pending: 'amber',
  succeeded: 'green',
  failed: 'red',
};

export const deliveryErrorLabels: Record<string, string> = {
  timeout: 'ปลายทางตอบช้าเกินกำหนด',
  redirect_not_followed: 'ปลายทางส่งต่อไปที่อื่น ระบบไม่ตามไป',
  address_blocked: 'ปลายทางเป็นที่อยู่ภายใน ระบบไม่ส่ง',
  connection_failed: 'เชื่อมต่อปลายทางไม่ได้',
  secret_unavailable: 'อ่าน secret ไม่ได้ (กุญแจเข้ารหัสของเซิร์ฟเวอร์เปลี่ยน)',
  subscription_missing: 'ไม่พบ webhook',
  webhook_disabled: 'webhook ถูกปิด ระบบหยุดส่ง',
  webhook_deleted: 'webhook ถูกลบ ระบบหยุดส่ง',
};
