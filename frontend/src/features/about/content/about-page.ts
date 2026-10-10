import type { HeroAction, SlotImage } from '@/features/home/types/home.types';

/**
 * Everything the "about the clinic" page says that does not come from the API. Edit the wording
 * here. The branches, opening hours and holidays are read from the API, so they change when the
 * clinic changes its settings, not here.
 *
 * The photos are from Unsplash, under the Unsplash License (free to use, no credit required):
 *   vet-exam-dog.jpg     https://unsplash.com/photos/u2H8mUzoF2Q
 *   cat-in-carrier.jpg   https://unsplash.com/photos/OBu_nabEpAw
 */

export const aboutMeta = {
  title: 'เกี่ยวกับคลินิก | Happy Paws Clinic',
  description: 'เรื่องราว ทีม เครื่องมือ สาขา และเวลาทำการของ Happy Paws คลินิกสัตว์เล็กย่านสุขุมวิท',
};

export const aboutHero = {
  eyebrow: 'เกี่ยวกับคลินิก',
  heading: ['คลินิกสัตว์เล็ก', 'ของคนในย่านสุขุมวิท'],
  lead: 'เราเริ่มจากห้องตรวจห้องเดียวเมื่อ 15 ปีก่อน วันนี้มีสัตวแพทย์ประจำ ห้องแล็บ และห้องผ่าตัดของเราเอง แต่ยังตั้งใจทำสิ่งเดิม คือรู้จักน้องแต่ละตัวและคุยกับเจ้าของให้เข้าใจก่อนทุกครั้ง',
  image: { src: '/images/about/vet-exam-dog.jpg', alt: 'สัตวแพทย์ตรวจสุนัขบนโต๊ะตรวจ' } satisfies SlotImage,
};

export const aboutStory = {
  heading: 'เรื่องราวของเรา',
  paragraphs: [
    'Happy Paws เปิดในปี 2554 ในห้องแถวเล็ก ๆ ปากซอยสุขุมวิท 39 ด้วยสัตวแพทย์สองคนที่อยากให้คลินิกแถวบ้านเป็นที่ที่เจ้าของกล้าถาม และสัตว์ไม่กลัวที่จะมา',
    'เราโตตามคนไข้ของเรา จากวัคซีนและตรวจสุขภาพ มาเป็นแล็บตรวจเลือดที่รู้ผลในคลินิก ห้องผ่าตัด และระบบนัดออนไลน์ที่ให้เจ้าของดูคิวและประวัติของน้องได้เองจากมือถือ',
  ],
  image: { src: '/images/about/cat-in-carrier.jpg', alt: 'แมวนั่งรออยู่ในกระเป๋าใส่สัตว์' } satisfies SlotImage,
  milestonesLabel: 'เส้นทางของคลินิก',
  milestones: [
    { year: '2554', text: 'เปิดคลินิกห้องตรวจเดียว ปากซอยสุขุมวิท 39' },
    { year: '2559', text: 'เปิดห้องแล็บ รู้ผลตรวจเลือดภายใน 30 นาที' },
    { year: '2563', text: 'เปิดห้องผ่าตัดและห้องพักฟื้นแยกสุนัขกับแมว' },
    { year: '2569', text: 'จองนัด ดูคิว และดูประวัติสุขภาพออนไลน์ได้' },
  ],
};

export const aboutValues = {
  heading: 'สิ่งที่เรายึดถือ',
  lead: 'สี่ข้อที่ทุกคนในทีมทำเหมือนกัน ไม่ว่าน้องจะมาฉีดวัคซีนหรือมาผ่าตัด',
};

/** The rooms and equipment, in the order they are shown. */
export const aboutFacilities = {
  heading: 'ห้องและเครื่องมือในคลินิก',
  lead: 'ตรวจ วินิจฉัย และรักษาได้จบในที่เดียว ไม่ต้องพาน้องเดินทางหลายที่',
  items: [
    { title: 'ห้องตรวจ', detail: 'แยกเป็นห้อง ๆ เงียบพอให้น้องไม่ตื่นกลัว และเจ้าของอยู่ด้วยได้ตลอด' },
    { title: 'แล็บในคลินิก', detail: 'ตรวจเลือด ปัสสาวะ และอุจจาระ รู้ผลระหว่างรอ ไม่ต้องนัดมาฟังผลอีกวัน' },
    { title: 'ห้องผ่าตัด', detail: 'เครื่องดมยาสลบและเครื่องติดตามสัญญาณชีพตลอดการผ่าตัด' },
    { title: 'เอกซเรย์ดิจิทัล', detail: 'เห็นภาพทันที และส่งภาพให้เจ้าของดูย้อนหลังได้' },
    { title: 'ห้องพักฟื้น', detail: 'แยกฝั่งสุนัขกับแมว ลดความเครียดระหว่างนอนดูอาการ' },
    { title: 'มุมรอสำหรับแมว', detail: 'ที่นั่งรอแยกจากสุนัข และมีที่วางกระเป๋าใส่แมวยกสูงจากพื้น' },
  ],
};

export const aboutVisit = {
  heading: 'สาขาและเวลาทำการ',
  branchesLabel: 'สาขา',
  roomsLabel: (count: number) => `ห้องตรวจ ${count} ห้อง`,
  noAddress: 'ดูที่อยู่และแผนที่ได้ที่หน้าแรก',
  mapLink: { label: 'ดูแผนที่และการเดินทาง', href: '/#contact' } satisfies HeroAction,
  hoursLabel: 'เวลาทำการ',
  today: 'วันนี้',
  closed: 'ปิดทำการ',
  hoursRange: (opens: string, closes: string) => `${opens} - ${closes} น.`,
  notConfigured: 'คลินิกกำลังปรับเวลาทำการ โทรสอบถามได้ที่ 02-123-4567',
  holidaysLabel: 'วันหยุดพิเศษที่จะถึง',
  wholeClinic: 'ทุกสาขา',
  unavailable: 'ยังโหลดข้อมูลสาขาและเวลาทำการไม่ได้ ลองใหม่อีกครั้งภายหลัง หรือโทร 02-123-4567',
};

export const aboutLicence = {
  heading: 'ข้อมูลสถานพยาบาล',
  lead: 'คลินิกได้รับอนุญาตประกอบกิจการสถานพยาบาลสัตว์ ตรวจสอบได้จากข้อมูลด้านล่าง',
};

export const aboutCta = {
  heading: 'อยากรู้จักทีมที่จะดูแลน้อง?',
  lead: 'ดูประวัติสัตวแพทย์และทีมงาน หรือจองนัดเลือกหมอและเวลาที่สะดวกได้เลย',
  primaryAction: { label: 'จองนัดหมาย', href: '/book' } satisfies HeroAction,
  secondaryAction: { label: 'ทีมของเรา', href: '/team' } satisfies HeroAction,
};
