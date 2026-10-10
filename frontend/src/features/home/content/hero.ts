import type { HeroAction, SlotImage } from '../types/home.types';

/** The words and links of the hero. Edit the page's opening here, not in the components. */
export const hero = {
  headingLines: ['ดูแลเพื่อนรัก', 'ให้ดีที่สุด'],
  /** Follows the last heading line, in the accent colour. */
  headingAccent: 'ทุกวัน',

  lead: 'คลินิกสัตว์เล็กย่านสุขุมวิท ดูแลสุนัขและแมวโดยทีมสัตวแพทย์ประจำ 4 ท่าน มีห้องแล็บในคลินิก จองคิวออนไลน์ได้ 24 ชั่วโมง',

  // Signed out, the booking page sends the visitor to sign in first.
  primaryAction: { label: 'จองนัดหมาย', href: '/book' } satisfies HeroAction,
  secondaryAction: { label: 'ดูบริการและราคา', href: '/services' } satisfies HeroAction,

  socialProof: {
    badge: '9k+',
    lines: ['น้องหมาน้องแมวกว่า 9,400 ตัว ได้รับการดูแลจากเรา', 'คะแนนรีวิว 4.9 จาก 1,240 ครอบครัว'],
  },
};

// The banner picture (the dog and the cat). Put the file in public/images/home/ and set
// `src: '/images/home/<file>'`; until then the hero shows an empty slot in its place.
export const heroImage: SlotImage = { alt: 'สุนัขโกลเด้นรีทรีฟเวอร์และแมวสีเทา' };
