import type { CareStep, WarningSign } from '../types/emergency.types';

/**
 * What the emergency page (`/emergency`) tells an owner about their pet's health: the signs that
 * need a vet at once, what to do on the way, and what not to do.
 *
 * MEDICAL CONTENT. A veterinarian at the clinic must check and approve this file before the page
 * goes live, and again after every change. Fill in `clinicalReview` when they have; until then the
 * page says the advice is awaiting the clinic vet's review, as the vaccine schedule page does. See
 * docs/features/emergency-page.md.
 *
 * What it is: short, general first-aid guidance of the kind veterinary emergency services publish
 * for owners. It names no medicines and no doses, and it never replaces a call to the clinic: every
 * item ends in "call and come in". Points for the vet to confirm or change:
 * - The list of signs, and the examples given for each (poisons, breathing, urinary blockage in
 *   male cats, bloat in large deep-chested dogs, a birth with strong straining for 30 minutes).
 * - "Keep warm" on the way, with heat stroke as the exception: cool with ordinary tap water and
 *   moving air, never ice water.
 * - Pressing a clean cloth on a bleeding wound, and laying a fresh one on top of a soaked one.
 * - Carrying a pet that may have a broken bone or a hurt back on a board or a large cloth.
 * - Seizures: nothing in the mouth, hard and sharp things moved away, lights dimmed, start noted.
 * - Paracetamol named as dangerous to cats, as the example of a human medicine.
 */

/** Who approved this advice and when. Leave both empty until a clinic vet has. */
export const clinicalReview: { reviewedBy?: string; reviewedOn?: string } = {};

/** Signs that need a vet right away, in the order the page lists them. */
export const warningSigns: WarningSign[] = [
  {
    id: 'poison',
    title: 'กินของมีพิษหรือของที่ไม่ควรกิน',
    detail: 'เช่น ยาของคน ยาเบื่อหนู ยาฆ่าแมลง น้ำยาทำความสะอาด ช็อกโกแลต องุ่น ลูกเกด แม้น้องยังดูปกติดี',
  },
  {
    id: 'breathing',
    title: 'หายใจลำบาก',
    detail: 'หอบแรง หายใจเสียงดัง แมวอ้าปากหายใจ หรือเหงือกและลิ้นเป็นสีม่วงหรือซีด',
  },
  {
    id: 'seizure',
    title: 'ชัก',
    detail: 'ตัวเกร็ง กระตุก ไม่รู้สึกตัว หรือชักซ้ำหลายครั้ง แม้หยุดชักแล้วก็ต้องให้หมอตรวจ',
  },
  {
    id: 'bleeding',
    title: 'เลือดออกมาก',
    detail: 'กดแผลไว้แล้วเลือดยังไม่หยุด แผลลึกหรือกว้าง หรือมีเลือดออกจากปาก จมูก หรือก้น',
  },
  {
    id: 'accident',
    title: 'ถูกรถชนหรือตกจากที่สูง',
    detail: 'แม้ดูไม่มีแผลและยังเดินได้ อาจบาดเจ็บภายในที่มองไม่เห็น',
  },
  {
    id: 'urine',
    title: 'ฉี่ไม่ออก',
    detail: 'เบ่งฉี่บ่อยแต่ไม่มีฉี่หรือออกเพียงหยด ร้องเวลาเบ่ง พบบ่อยในแมวตัวผู้ และอันตรายถึงชีวิตได้ในไม่กี่ชั่วโมง',
  },
  {
    id: 'bloat',
    title: 'ท้องอืดแข็งในสุนัข',
    detail: 'ท้องโป่งตึง พยายามอาเจียนแต่ไม่มีอะไรออก น้ำลายไหล กระสับกระส่าย พบบ่อยในสุนัขพันธุ์ใหญ่อกลึก',
  },
  {
    id: 'collapse',
    title: 'ล้มฟุบ หมดสติ หรือลุกไม่ขึ้น',
    detail: 'ซึมมาก ไม่ตอบสนองเมื่อเรียก ตัวเย็น หรือเหงือกซีดขาว',
  },
  {
    id: 'eye',
    title: 'บาดเจ็บที่ตา',
    detail: 'ตาโปน ตาถูกข่วนหรือแทง หลับตาข้างเดียวแล้วเจ็บ หรือมีสารเคมีเข้าตา',
  },
  {
    id: 'heat',
    title: 'ฮีทสโตรก',
    detail: 'หอบหนัก น้ำลายยืด ตัวร้อนจัด อ่อนแรง หรืออาเจียน หลังอยู่กลางแดด ในที่ร้อน หรือในรถที่จอดไว้',
  },
  {
    id: 'birth',
    title: 'คลอดยาก',
    detail: 'เบ่งแรงนานเกิน 30 นาทีแล้วลูกยังไม่ออก หรือมีเลือดหรือน้ำสีเขียวเข้มออกมาโดยยังไม่มีลูก',
  },
];

/** What to do on the way to the clinic. */
export const onTheWay: CareStep[] = [
  {
    id: 'call',
    title: 'โทรหาคลินิกก่อนออกจากบ้าน',
    detail: 'บอกอาการและเวลาที่จะถึง ทีมจะบอกว่าต้องทำอะไรระหว่างทาง และเตรียมรับน้องไว้',
  },
  {
    id: 'still',
    title: 'ให้น้องอยู่นิ่งที่สุด',
    detail: 'อุ้มหรือใส่กล่องหรือกรงอย่างเบามือ ถ้ากระดูกหักหรือบาดเจ็บหลัง วางบนแผ่นกระดานหรือผ้าผืนใหญ่แล้วยกทั้งผืน น้องที่เจ็บอาจกัดได้แม้เป็นเจ้าของ',
  },
  {
    id: 'warm',
    title: 'ให้น้องอบอุ่น',
    detail: 'ห่มผ้าบาง ๆ ยกเว้นน้องที่เป็นฮีทสโตรก ให้ใช้ผ้าชุบน้ำธรรมดาเช็ดตัว เปิดแอร์หรือพัดลมในรถ ห้ามใช้น้ำแข็ง',
  },
  {
    id: 'packaging',
    title: 'ถ้าน้องกินอะไรเข้าไป เอาสิ่งนั้นมาด้วย',
    detail: 'นำกล่อง ฉลาก ซอง หรือของที่เหลือมาให้หมอดู และจำเวลาที่น้องกินไว้',
  },
  {
    id: 'pressure',
    title: 'ถ้ามีเลือดออก ใช้ผ้าสะอาดกดแผลไว้',
    detail: 'กดค้างไว้ตลอดทาง ถ้าผ้าชุ่มเลือด ให้วางผืนใหม่ทับลงไป ไม่ต้องดึงผืนเดิมออก',
  },
];

/** What not to do. */
export const doNot: CareStep[] = [
  {
    id: 'vomit',
    title: 'อย่าทำให้น้องอาเจียนเอง',
    detail: 'ถ้าสัตวแพทย์ยังไม่ได้แนะนำ สารบางอย่างทำร้ายหลอดอาหารซ้ำตอนอาเจียน และวิธีที่ใช้กันเองอาจเป็นพิษกับน้อง',
  },
  {
    id: 'medicine',
    title: 'อย่าให้ยาของคน',
    detail: 'ยาแก้ปวดและยาลดไข้ของคนเป็นพิษต่อสัตว์ได้ พาราเซตามอลแม้เม็ดเดียวก็อันตรายมากต่อแมว',
  },
  {
    id: 'mouth',
    title: 'อย่าเอามือหรือสิ่งของใส่ปากน้องตอนชัก',
    detail: 'น้องไม่กลืนลิ้น ให้ย้ายของแข็งหรือของมีคมออกห่าง หรี่ไฟ แล้วจดเวลาที่เริ่มชัก',
  },
  {
    id: 'wait',
    title: 'อย่ารอดูอาการข้ามคืน',
    detail: 'อาการในรายการด้านบนแย่ลงได้เร็ว ถ้าไม่แน่ใจ โทรถามคลินิกก่อนได้เสมอ',
  },
];
