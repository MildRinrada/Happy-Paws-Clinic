import type { FeaturedService, MoreService } from '../types/home.types';

export const servicesHeading = 'ครบตั้งแต่ป้องกัน ตรวจไปจนถึงรักษา';

/** Under the cards: the full menu with today's prices, read from the API. */
export const servicesMoreLink = { label: 'ดูบริการและราคาทั้งหมด', href: '/services' };

// Each card's picture: put the file in public/images/home/ and set `src: '/images/home/<file>'`.
// Without `src` the card shows an empty slot where the picture goes.

/** The three large cards, left to right. */
export const featuredServices: FeaturedService[] = [
  {
    id: 'checkup',
    name: 'ตรวจสุขภาพทั่วไป',
    price: '฿500',
    description: ['ขูดหินปูน ขัดฟัน ถอนฟันน้ำนม', 'ดมยาสลบโดยทีมวิสัญญีเฉพาะทาง'],
    tags: [
      { label: '30 นาที', tone: 'neutral' },
      { label: 'งดอาหาร 8 ชม.', tone: 'warning' },
    ],
    image: { alt: 'สัตวแพทย์ตรวจสุขภาพสุนัข' },
  },
  {
    id: 'vaccine',
    name: 'วัคซีนและป้องกันโรค',
    price: '฿650',
    description: ['วัคซีนรวม พิษสุนัขบ้า ถ่ายพยาธิ ป้องกันเห็บหมัด', 'พร้อมเตือนรอบถัดไป'],
    tags: [
      { label: '20 นาที', tone: 'neutral' },
      { label: 'มีเตือนอัตโนมัติ', tone: 'notice' },
    ],
    image: { alt: 'สัตวแพทย์ฉีดวัคซีนให้สุนัข' },
  },
  {
    id: 'dental',
    name: 'ทันตกรรม',
    price: '฿2,500',
    description: ['ขูดหินปูน ขัดฟัน ถอนฟันน้ำนม', 'ดมยาสลบโดยทีมวิสัญญีเฉพาะทาง'],
    tags: [
      { label: 'ครึ่งวัน', tone: 'neutral' },
      { label: 'งดอาหาร 8 ชม.', tone: 'warning' },
    ],
    image: { alt: 'สัตวแพทย์ตรวจฟันสุนัข' },
  },
];

/** The three compact cards underneath. `icon` names a drawing in components/services/icons. */
export const moreServices: MoreService[] = [
  { id: 'surgery', name: 'ผ่าตัดและทำหมัน', detail: 'ห้องผ่าตัดปลอดเชื้อ เริ่ม ฿1,800', icon: 'surgery' },
  { id: 'lab', name: 'ตรวจเลือดและแล็บ', detail: 'รู้ผลใน 30 นาที · เริ่ม ฿800 เริ่ม ฿1,800', icon: 'lab' },
  { id: 'grooming', name: 'อาบน้ำ ตัดขน', detail: 'มีสัตวแพทย์ดูแลใกล้ชิด · เริ่ม ฿450', icon: 'grooming' },
];
