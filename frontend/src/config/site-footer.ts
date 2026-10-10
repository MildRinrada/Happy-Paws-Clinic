import type { SiteLink } from './site-navigation';

/**
 * Everything the public site's footer says. Edit the footer here, not in its components.
 *
 * A link whose destination does not exist yet points at '#'; give it a real `href` when it does.
 */

/** A label with the value under it. `lines` is the value, one entry per line. */
export interface FooterDetail {
  label: string;
  lines: string[];
  /** Makes the value a link — `tel:`, `mailto:` or a page. */
  href?: string;
}

export type SocialIconName = 'instagram' | 'line' | 'facebook';

export interface SocialLink {
  name: string;
  icon: SocialIconName;
  href: string;
}

// The phone numbers, email, chat accounts, map and opening hours are not here. They come from the
// API (`GET /clinic/contacts`, `GET /clinic/hours`) and are edited in the staff console under
// ข้อมูลติดต่อ, so the footer, the home page and the contact page always say the same thing.

/** The red bar above the footer. The number is the clinic's "emergency" contact entry. */
export const emergency = {
  label: 'สายด่วนฉุกเฉิน 24 ชม',
  note: 'กรณีสัตว์ได้รับอุบัติเหตุ ชัก หายใจลำบาก หรือถูกสัตว์อื่นกัด โทรก่อนเข้ามาได้ทันทีไม่ต้องนัด',
  /** After the note: the page of signs that need a vet now and what to do on the way. */
  more: { label: 'อาการแบบไหนต้องรีบมา', href: '/emergency' } satisfies SiteLink,
};

/** First column: where the clinic is. The address and link are the clinic's "map" contact entry. */
export const footerAddress = {
  // The small map picture, drawn when the map entry has no Google Maps embed. Put the file in
  // public/images/home/ and set `imageSrc: '/images/home/<file>'`.
  map: { alt: 'แผนที่คลินิก' } as { imageSrc?: string; alt: string },
  mapLinkLabel: 'เปิดใน Google Map',
};

/** Second column. */
export const footerContact = {
  title: 'ติดต่อคลินิก',
  chatLabel: 'ช่องทางแชท',
  more: { label: 'ช่องทางติดต่อทั้งหมด', href: '/contact' } satisfies SiteLink,
};

/** Third column. */
export const footerHours = {
  title: 'เวลาทำการ',
  closed: 'ปิดทำการ',
  unavailable: 'โทรสอบถามเวลาทำการ',
};

/** Fourth column: the licences a veterinary clinic must show. */
export const footerLegal = {
  title: 'ข้อมูลสถานพยาบาล',
  details: [
    { label: 'ใบอนุญาตประกอบกิจการสถานพยาบาลสัตว์', lines: ['เลขที่ 10-2554-0187'] },
    { label: 'ผู้ควบคุมกิจการ / ผู้อำนวยการคลินิก', lines: ['น.สพ. กิตติ วรกุล · ใบประกอบวิชาชีพ', 'ส.พ. 04412'] },
    { label: 'ผู้ดำเนินการสถานพยาบาล', lines: ['สพ.ญ. มินท์ ศรีสุข · ใบประกอบวิชาชีพ', 'ส.พ. 06120'] },
    { label: 'ทะเบียนนิติบุคคล', lines: ['0105554021873'] },
  ] satisfies FooterDetail[],
};

/** The row of links under the divider. */
export const footerLinks: SiteLink[] = [
  { label: 'บริการและราคา', href: '/services' },
  { label: 'เกี่ยวกับคลินิก', href: '/about' },
  { label: 'ทีมของเรา', href: '/team' },
  { label: 'บทความสุขภาพสัตว์', href: '/articles' },
  { label: 'ตารางวัคซีนน้อง', href: '/vaccine-schedule' },
  { label: 'จองนัดหมาย', href: '/book' },
  { label: 'กรณีฉุกเฉิน', href: '/emergency' },
  { label: 'ร่วมงานกับเรา', href: '#' },
];

/** The black bar at the very bottom. */
export const copyright = {
  text: 'Copyright © 2569 (2026) Happy Paws Veterinary Clinic Co., Ltd. สงวนลิขสิทธิ์ทุกประการ',
  privacyLink: { label: 'นโยบายความเป็นส่วนตัว', href: '/privacy' } satisfies SiteLink,
};
