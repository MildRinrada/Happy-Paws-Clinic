import type { CarePromise, SlotImage } from '../types/home.types';

export const careHeading = 'ดูแลเหมือนน้องเป็นสมาชิกในบ้านเรา';

/** The numbered promises, top to bottom. The number shown is the position in this list. */
export const carePromises: CarePromise[] = [
  { title: 'คุยกันก่อนทุกครั้ง', detail: 'อธิบายอาการ ทางเลือกการรักษา และค่าใช้จ่ายให้ครบก่อนเริ่ม' },
  { title: 'ทีมสัตวแพทย์ประจำ', detail: 'น้องได้พบหมอคนเดิมที่จำประวัติและนิสัยของน้องได้' },
  { title: 'ประวัติสุขภาพออนไลน์', detail: 'ดูผลเลือด วัคซีน ยาที่จ่าย และสรุปการตรวจย้อนหลังได้จากมือถือ' },
  { title: 'ไม่ต้องรอเก้อ', detail: 'จองคิวออนไลน์ เช็กคิวสดก่อนออกจากบ้าน รับแจ้งเตือนผ่าน LINE' },
];

// The picture beside the list. Put the file in public/images/home/ and set
// `src: '/images/home/<file>'`; until then the section shows an empty slot in its place.
export const careImage: SlotImage = { alt: 'สัตวแพทย์อุ้มลูกสุนัข' };
