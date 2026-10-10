import type { QueueDisplayStatus } from '../types/queue.types';

/** The console's page for waiting-room screens (docs/contract-extensions.md § 30). */
export const queueDisplays = {
  metaTitle: 'จอห้องรอ · Happy Paws Clinic',
  title: 'จอห้องรอ',
  subtitle: (branch: string) => `${branch} · จอทีวีที่เปิดคิวได้โดยไม่ต้องเข้าสู่ระบบด้วยบัญชีพนักงาน`,
  back: 'กลับไปกระดานคิว',
  noBranch: 'ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์จัดการจอห้องรอ (queue:write)',

  intro:
    'จอแต่ละเครื่องได้รหัสของตัวเอง ใช้เปิดคิวของสาขานี้ได้อย่างเดียว ไม่เห็นชื่อสัตว์หรือเจ้าของ และเปิดหน้าอื่นของระบบไม่ได้ ยกเลิกเมื่อไรจอหยุดทันที',

  form: {
    heading: 'เพิ่มจอ',
    name: 'ชื่อจอ',
    nameHint: 'เช่น ทีวีห้องรอชั้น 1 ไว้แยกจอในรายการ',
    expiry: 'ใช้ได้ถึง',
    expiryHint: 'จอบนผนังส่วนใหญ่ตั้งไว้จนกว่าจะยกเลิก',
    submit: 'สร้างรหัสตั้งจอ',
    pending: 'กำลังสร้าง…',
  },

  expiryOptions: [
    { days: 0, label: 'จนกว่าจะยกเลิก' },
    { days: 30, label: '30 วัน' },
    { days: 90, label: '90 วัน' },
    { days: 365, label: '1 ปี' },
  ],

  created: {
    heading: (name: string) => `รหัสตั้งจอ "${name}"`,
    warning: 'รหัสนี้แสดงครั้งเดียว ใช้ได้ครั้งเดียวภายใน 30 นาที ถ้าหมดเวลาให้ยกเลิกจอนี้แล้วสร้างใหม่',
    steps: [
      'บนทีวี เปิดเบราว์เซอร์ไปที่ลิงก์ตั้งจอ หรือเปิดหน้า /display/setup แล้วพิมพ์รหัส',
      'จอเปิดคิวเองเมื่อรหัสถูก แล้วกดเต็มจอ เช่น F11 หรือโหมด kiosk',
      'ไม่ต้องเข้าสู่ระบบบนทีวี และไม่ต้องจดรหัสเก็บไว้',
    ],
    code: 'รหัสตั้งจอ',
    link: 'ลิงก์ตั้งจอ',
    qr: 'QR ของลิงก์ตั้งจอ สำหรับจอที่สแกนได้ เช่น แท็บเล็ต',
    until: (time: string) => `ใช้ได้ถึง ${time}`,
    done: 'เสร็จแล้ว',
  },

  list: {
    heading: 'จอของสาขานี้',
    empty: 'ยังไม่มีจอ',
    columns: { name: 'จอ', status: 'สถานะ', dates: 'เวลา' },
    createdAt: (time: string) => `สร้าง ${time}`,
    claimedAt: (time: string) => `ตั้งบนทีวี ${time}`,
    setupUntil: (time: string) => `รหัสใช้ได้ถึง ${time}`,
    lastSeen: (time: string) => `เห็นล่าสุด ${time}`,
    neverSeen: 'ยังไม่เคยเปิด',
    expires: (time: string) => `หมดอายุ ${time}`,
    noExpiry: 'ไม่มีวันหมดอายุ',
    revokedAt: (time: string) => `ยกเลิก ${time}`,
  },

  status: {
    pending_setup: 'รอตั้งบนทีวี',
    setup_expired: 'รหัสหมดเวลา',
    active: 'ใช้งานอยู่',
    expired: 'หมดอายุ',
    revoked: 'ยกเลิกแล้ว',
  } satisfies Record<QueueDisplayStatus, string>,

  revoke: {
    button: 'ยกเลิกจอ',
    confirm: 'จอนี้จะหยุดแสดงคิวภายในไม่เกิน 1 นาที และใช้อีกไม่ได้ ต้องสร้างจอใหม่',
    confirmButton: 'ยกเลิกจอนี้',
    pending: 'กำลังยกเลิก…',
    back: 'ไม่ยกเลิก',
  },
} as const;
