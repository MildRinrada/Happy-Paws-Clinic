import type { Testimonial } from '../types/home.types';

export const testimonialsHeading = 'เสียงจากครอบครัวน้อง ๆ';

/** What owners said, top to bottom. Write the quote without quote marks; the card adds them. */
export const testimonials: Testimonial[] = [
  {
    id: 'milo',
    quote: ['จองคิวออนไลน์แล้วไปถึงได้ตรวจเลย', 'ไม่ต้องนั่งรอเป็นชั่วโมงเหมือนที่เก่า หมออธิบายละเอียดมาก'],
    author: 'คุณมายด์',
    relation: 'เจ้าของไมโล',
  },
  {
    id: 'pangpang',
    quote: ['จองคิวออนไลน์แล้วไปถึงได้ตรวจเลย', 'ไม่ต้องนั่งรอเป็นชั่วโมงเหมือนที่เก่า หมออธิบายละเอียดมาก'],
    author: 'คุณเบล',
    relation: 'เจ้าของปังปัง',
  },
];
