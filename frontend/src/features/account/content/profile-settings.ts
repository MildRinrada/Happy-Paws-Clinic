import type { NotificationChannels } from '../types/account.types';

/** The "ข้อมูลส่วนตัว" card on the staff account page. */
export const profileSettings = {
  heading: 'ข้อมูลส่วนตัว',
  displayName: 'ชื่อที่แสดง',
  displayNameHint: 'ชื่อนี้ขึ้นในเวชระเบียนที่ลงนามต่อจากนี้ ไม่เกิน 200 ตัวอักษร',
  email: 'อีเมล',
  emailHint: 'ใช้เข้าสู่ระบบ เปลี่ยนแล้วต้องใช้อีเมลใหม่ทันที',
  phone: 'เบอร์โทรศัพท์ (ไม่บังคับ)',
  phoneHint: 'ตัวเลข 9 ถึง 15 หลัก เว้นว่างเพื่อลบ',
  language: 'ภาษาของข้อความที่ส่งถึงคุณ',
  languages: { th: 'ไทย', en: 'English' } satisfies Record<'th' | 'en', string>,
  channels: 'ช่องทางที่สะดวกรับข้อความ',
  channelsHint: 'ระบบบันทึกค่านี้ไว้ ยังไม่ได้ใช้คัดข้อความที่ส่งจริง',
  channelNames: { email: 'อีเมล', sms: 'SMS', line: 'LINE' } satisfies Record<keyof NotificationChannels, string>,
  currentPassword: 'รหัสผ่านปัจจุบัน',
  currentPasswordHint: 'ต้องใส่เมื่อเปลี่ยนอีเมลหรือเบอร์โทร เพื่อยืนยันว่าเป็นคุณ',
};
