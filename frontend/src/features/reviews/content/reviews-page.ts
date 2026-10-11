import type { ReviewFilter } from '../types/review-list.types';

/** The public `/reviews` page and the review cards on the site. */
export const reviewsPage = {
  metaTitle: 'รีวิวจากลูกค้าจริง | Happy Paws Clinic',
  metaDescription: 'รีวิวจากเจ้าของสุนัขและแมวที่มาใช้บริการจริง คลินิกตรวจทุกรีวิวก่อนขึ้นเว็บ และไม่ลบรีวิวเพราะคะแนนต่ำ',
  eyebrow: 'รีวิว',
  title: 'รีวิวจากลูกค้าจริง',
  /** The reasons match the only ones staff can pick when taking a review down (docs/features/reviews.md). */
  intro: 'ทุกรีวิวมาจากลูกค้าที่มาใช้บริการจริง คลินิกอ่านก่อนขึ้นหน้าเว็บ และนำออกเฉพาะรีวิวที่มีข้อมูลส่วนบุคคล ภาษาไม่สุภาพ ไม่เกี่ยวกับบริการ สแปม หรือเมื่อเจ้าของรีวิวขอ ไม่นำออกเพราะคะแนนต่ำ',

  empty: 'ยังไม่มีรีวิวที่เผยแพร่',
  emptyHint: 'รีวิวจะขึ้นที่นี่หลังคลินิกอ่านแล้ว ถ้าเคยพาน้องมาที่คลินิก เขียนรีวิวเป็นคนแรกได้เลย',
  unavailable: 'โหลดรีวิวไม่ได้ในขณะนี้ ลองใหม่อีกครั้ง',

  average: 'คะแนนเฉลี่ย',
  outOf: 'จาก 5',
  averageLabel: (average: string) => `คะแนนเฉลี่ย ${average} จาก 5 ดาว`,
  count: (count: number) => `จาก ${count.toLocaleString('th-TH')} รีวิว`,
  stars: (stars: number) => `${stars} ดาว`,
  barLabel: (stars: number, count: number) => `${stars} ดาว ${count.toLocaleString('th-TH')} รีวิว`,
  ratingLabel: (rating: number) => `${rating} จาก 5 ดาว`,

  owner: (species?: string | null) => (species ? `เจ้าของ${species}` : 'เจ้าของสัตว์เลี้ยง'),
  reply: 'คลินิกตอบ',

  next: 'รีวิวก่อนหน้านี้',

  /** The link under the home page's review column, and back to every review from an empty filter. */
  more: 'ดูรีวิวทั้งหมด',
};

/** The chips over the list. */
export const reviewFilters = {
  /** Read out for the row of chips. */
  label: 'กรองรีวิว',
  chips: {
    all: 'ทั้งหมด',
    '5': '5 ดาว',
    '4': '4 ดาว',
    '3down': '3 ดาวลงไป',
    replied: 'มีคำตอบจากคลินิก',
  } satisfies Record<ReviewFilter, string>,
  /** Nothing under a filter. */
  empty: {
    all: 'ยังไม่มีรีวิวที่เผยแพร่',
    '5': 'ยังไม่มีรีวิว 5 ดาว',
    '4': 'ยังไม่มีรีวิว 4 ดาว',
    '3down': 'ยังไม่มีรีวิว 3 ดาวลงไป',
    replied: 'ยังไม่มีรีวิวที่คลินิกตอบ',
  } satisfies Record<ReviewFilter, string>,
  /** Under the chips while a filter can only look at the newest reviews. */
  coversLatest: (count: number) => `ตัวกรองนี้ดูจากรีวิวล่าสุด ${count.toLocaleString('th-TH')} รีวิว`,
};

/** The card under the average: how to write a review. */
export const writeReviewCard = {
  heading: 'เคยพาน้องมาที่คลินิก?',
  body: 'เขียนรีวิวได้จากบัญชีของคุณ หรือจากลิงก์ที่คลินิกส่งให้หลังมาใช้บริการ',
  action: 'เขียนรีวิว',
};
