import type { BadgeTone } from '@/components/ui/badge';

/** How the backend's delivery words read on screen. Keys are the API's own values. */

export const channelLabels: Record<string, string> = {
  line: 'LINE',
  sms: 'SMS',
  email: 'อีเมล',
};

export const statusLabels: Record<string, string> = {
  sent: 'ส่งแล้ว',
  failed: 'ส่งไม่สำเร็จ',
  skipped: 'ไม่ได้ส่ง',
  unknown: 'ไม่ทราบผล',
};

export const statusTones: Record<string, BadgeTone> = {
  sent: 'green',
  failed: 'red',
  skipped: 'amber',
  unknown: 'neutral',
};

export const sourceLabels: Record<string, string> = {
  system: 'ระบบส่งเอง',
  manual: 'พนักงานส่ง',
  broadcast: 'ประกาศ',
  automation: 'เตือนอัตโนมัติ',
};

/** The reasons in `error_code`, said the way the counter would explain them. */
export const errorLabels: Record<string, string> = {
  no_consent: 'ลูกค้าไม่ได้ยินยอมรับข้อความชนิดนี้',
  no_recipient: 'ไม่พบข้อมูลลูกค้า',
  no_address: 'ไม่มีช่องทางนี้ในข้อมูลลูกค้า',
  channel_not_configured: 'ยังไม่ได้ตั้งค่าช่องทางส่ง',
  channel_disabled: 'ปิดการส่งทาง LINE ไว้',
  template_disabled: 'ปิดข้อความชนิดนี้ทาง LINE ไว้',
  opted_out: 'ลูกค้าปิดช่องทางนี้เอง',
  unfollowed: 'ลูกค้าบล็อกหรือเลิกติดตาม LINE ของคลินิก',
  no_channel: 'ไม่มีช่องทางที่ส่งได้',
  provider_rejected: 'ผู้ให้บริการปฏิเสธข้อความ',
  provider_unauthorized: 'ผู้ให้บริการปฏิเสธรหัสเข้าใช้',
  provider_quota: 'โควตาของผู้ให้บริการหมด',
  provider_error: 'ผู้ให้บริการขัดข้อง',
  provider_unreachable: 'ติดต่อผู้ให้บริการไม่ได้',
};

/** Template keys by name. A key the backend adds later shows as itself until named here. */
export const templateLabels: Record<string, string> = {
  'appointment.reminder': 'เตือนนัดหมาย',
  'vaccine.due': 'ครบกำหนดวัคซีน',
  'treatment_plan.session': 'เตือนรอบแผนการรักษา',
  'clinic.message': 'ข้อความถึงเจ้าของ',
  'clinic.announcement': 'ประกาศ',
  'customer.lapsed': 'คิดถึงน้อง (ลูกค้าที่หายไป)',
  'queue.called': 'เรียกคิว',
  'receipt.issued': 'ใบเสร็จ',
  'consultation.summary': 'สรุปการตรวจ',
  'waiting_list.slot_available': 'มีคิวว่าง',
  'pet.deceased_appointments_cancelled': 'ยกเลิกนัดของสัตว์ที่เสียชีวิต',
  'customer.portal_invitation': 'ชวนเปิดบัญชีออนไลน์',
  'thread.reply': 'ตอบข้อความลูกค้า',
};

export const labelOf = (labels: Record<string, string>, key: string | undefined) => (key ? (labels[key] ?? key) : '-');
