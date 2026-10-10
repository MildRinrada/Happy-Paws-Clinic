import type { HeroAction } from '../types/home.types';

/** The orange band that closes the page: one question and two ways to answer it. */
export const bookingCta = {
  heading: 'พร้อมดูแลสุขภาพของเพื่อนรักแล้วหรือยัง?',
  lead: 'จองคิวออนไลน์ใน 1 นาที เลือกหมอและเวลาที่สะดวกได้เอง',

  // Signed out, the booking page sends the visitor to sign in first.
  primaryAction: { label: 'จองนัดหมาย', href: '/book' } satisfies HeroAction,
  secondaryAction: { label: 'เข้าสู่ระบบ', href: '/login' } satisfies HeroAction,
};
