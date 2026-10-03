import type { HeroAction, LineBenefit } from '../types/home.types';

/** The section inviting owners to add the clinic on LINE. */
export const line = {
  pill: 'เชื่อมต่อ LINE Official @happypaws',

  headingLines: ['ไม่ต้องเปิดเว็บ ก็รู้ทุกเรื่องของน้อง', 'ผ่าน LINE'],

  lead: 'แอดเพื่อนครั้งเดียว ระบบจะผูกกับบัญชีของคุณอัตโนมัติ แล้วส่งแจ้งเตือนทุกอย่างเข้าแชตที่คุณเปิดอยู่ทุกวัน',

  /** The numbered rows, top to bottom. The number shown is the position in this list. */
  benefits: [
    { text: 'เตือนก่อนถึงนัด 1 วัน และเช้าวันนัด · กดเลื่อนหรือยกเลิกได้ในแชตเลย' },
    { text: 'แจ้งเตือนคิว เมื่อเหลืออีก 1 คิวจะถึงน้อง · ไม่ต้องนั่งเฝ้าหน้าคลินิก' },
    { text: 'สรุปการตรวจ ใบเสร็จ ผลแล็บ และเตือนวัคซีน–ยาตามรอบ ส่งเข้าแชตทันที' },
  ] satisfies LineBenefit[],

  // Set `href` to the clinic's add-friend link (https://line.me/R/ti/p/@…) when there is one.
  action: { label: 'เพิ่มเพื่อนใน LINE', href: '#' } satisfies HeroAction,
};
