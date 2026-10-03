import type { ContactKind } from '../types/contact.types';

/** The staff page for the clinic's contact entries. */
export const contactSettings = {
  metaTitle: 'ข้อมูลติดต่อ · Happy Paws Clinic',
  title: 'ข้อมูลติดต่อ',
  subtitle: 'เบอร์โทร อีเมล แชท และแผนที่ ที่แสดงบนหน้าติดต่อเรา footer และหน้าแรกของเว็บ',
  noAccess: 'บัญชีนี้แก้ข้อมูลติดต่อไม่ได้ (settings:write)',
  viewPublic: 'ดูหน้าติดต่อเรา',
  empty: 'ยังไม่มีช่องทางติดต่อ เพิ่มรายการแรกได้ด้านล่าง',
  add: '+ เพิ่มช่องทางติดต่อ',
  columns: { kind: 'ประเภท', label: 'ชื่อที่แสดง', value: 'ข้อมูล', branch: 'สาขา', order: 'ลำดับ' },
  clinicWide: 'ทั้งคลินิก',
  edit: 'แก้ไข',
  remove: 'ลบ',
  confirmRemove: 'ยืนยันลบ',
  cancel: 'ยกเลิก',
  save: 'บันทึก',
  saving: 'กำลังบันทึก…',
  removing: 'กำลังลบ…',
  conflict: 'มีคนแก้รายการนี้ไปก่อนแล้ว โหลดหน้าใหม่เพื่อดูข้อมูลล่าสุด',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
  hasEmbed: 'มีแผนที่ฝัง',

  fields: {
    kind: 'ประเภท',
    label: 'ชื่อที่แสดง',
    labelHint: 'ข้อความเหนือข้อมูล เช่น โทรศัพท์คลินิก สายด่วนฉุกเฉิน',
    value: 'ข้อมูลที่แสดง',
    url: 'ลิงก์ (ไม่บังคับ)',
    urlHint: 'หน้าที่จะเปิดเมื่อกด เช่น หน้า Facebook ของคลินิก ลิงก์ Google Maps',
    embedUrl: 'ลิงก์ฝังแผนที่ (ไม่บังคับ)',
    embedUrlHint: 'ใน Google Maps กด แชร์ แล้ว ฝังแผนที่ คัดลอกเฉพาะลิงก์ใน src="..." ที่ขึ้นต้นด้วย https://www.google.com/maps/embed',
    branch: 'สาขา',
    order: 'ลำดับ',
    orderHint: 'เลขน้อยขึ้นก่อน',
  },

  /** Each kind's name, and what its "value" should hold. */
  kinds: {
    phone: { name: 'โทรศัพท์', valueHint: 'เช่น 02-123-4567 กดแล้วโทรออกได้' },
    emergency: { name: 'สายด่วนฉุกเฉิน', valueHint: 'เบอร์ที่ขึ้นในแถบสีแดงทุกหน้า' },
    email: { name: 'อีเมล', valueHint: 'เช่น care@happypaws.co.th' },
    line: { name: 'LINE', valueHint: 'เช่น @happypaws ใส่ลิงก์เพิ่มเพื่อให้กดแล้วเปิด LINE' },
    facebook: { name: 'Facebook', valueHint: 'ชื่อเพจ ใส่ลิงก์ของเพจด้วย' },
    instagram: { name: 'Instagram', valueHint: 'เช่น @happypaws.clinic ใส่ลิงก์ด้วย' },
    website: { name: 'เว็บไซต์', valueHint: 'ชื่อที่จะแสดง ใส่ลิงก์ด้วย' },
    map: { name: 'แผนที่', valueHint: 'ที่อยู่ที่แสดงใต้แผนที่' },
    other: { name: 'อื่น ๆ', valueHint: 'ข้อความที่จะแสดง' },
  } satisfies Record<ContactKind, { name: string; valueHint: string }>,
};
