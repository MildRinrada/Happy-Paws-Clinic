import type { ReviewNameChoice } from '../types/review.types';

/**
 * The words of the review form, shared by the portal and the invitation link. Edit them here, not
 * in the components.
 */
export const reviewForm = {
  rating: 'ให้คะแนน',
  ratingHint: 'แตะดาวเพื่อเลือก 1 ถึง 5 ดาว',
  ratingRequired: 'เลือกคะแนนก่อนส่ง',
  starLabel: (stars: number) => `${stars} ดาว`,
  starWords: ['', 'ต้องปรับปรุง', 'พอใช้', 'ดี', 'ดีมาก', 'ประทับใจมาก'] as const,

  text: 'เล่าให้คลินิกฟัง (ไม่บังคับ)',
  textHint: 'เช่น การนัด การรอ คำอธิบายของคุณหมอ ไม่ต้องใส่เบอร์โทรหรือข้อมูลส่วนตัว',
  textCount: (used: number, max: number) => `${used.toLocaleString('th-TH')} จาก ${max.toLocaleString('th-TH')} ตัวอักษร`,

  name: 'ชื่อที่แสดงบนหน้าเว็บ',
  nameChoices: {
    first_name: 'ชื่อจริงของฉัน (ชื่อแรกเท่านั้น ไม่มีนามสกุล)',
    anonymous: 'ไม่แสดงชื่อ (แสดงว่า ลูกค้า)',
  } satisfies Record<ReviewNameChoice, string>,
  firstNameAs: (name: string) => `จะแสดงว่า ${name}`,

  moderationNote: 'รีวิวจะขึ้นหน้าเว็บหลังคลินิกอ่านแล้วเท่านั้น และแก้ไขภายหลังไม่ได้ ถ้าต้องการให้นำออก ติดต่อคลินิกได้ทุกเมื่อ',

  submit: 'ส่งรีวิว',
  submitting: 'กำลังส่ง…',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',

  doneTitle: 'ขอบคุณสำหรับรีวิว',
  doneBody: 'คลินิกได้รับรีวิวแล้ว จะขึ้นหน้าเว็บหลังเจ้าหน้าที่อ่านแล้ว',
};

/** The longest review the API keeps, after cleaning. */
export const REVIEW_TEXT_MAX = 1000;
