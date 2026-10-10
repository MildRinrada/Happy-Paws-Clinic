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
    // The clinic's own claim: replace both with the clinic's real figure before launch.
    badge: '9k+',
    petsLine: 'น้องหมาน้องแมวกว่า 9,400 ตัว ได้รับการดูแลจากเรา',
    // Filled from the published reviews (`GET /reviews`), never typed in; left out while there are none.
    reviewsLine: (average: number, count: number) =>
      `คะแนนรีวิว ${average.toLocaleString('th-TH', { minimumFractionDigits: 1, maximumFractionDigits: 1 })} จาก ${count.toLocaleString('th-TH')} รีวิว`,
  },
};

// The banner picture (the dog and the cat). Put the file in public/images/home/ and set
// `src: '/images/home/<file>'`; until then the hero shows an empty slot in its place.
export const heroImage: SlotImage = { alt: 'สุนัขโกลเด้นรีทรีฟเวอร์และแมวสีเทา' };
