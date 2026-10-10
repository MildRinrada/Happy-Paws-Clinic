import type { ContactKind } from '@/features/clinic/types/contact.types';

/**
 * What the contact page says that does not come from the API. The phone numbers, chat accounts,
 * maps and hours are the clinic's own entries, edited in the staff console under ข้อมูลติดต่อ.
 */
export const contactPage = {
  meta: {
    title: 'ติดต่อเรา | Happy Paws Clinic',
    description: 'เบอร์โทร สายด่วนฉุกเฉิน อีเมล LINE แผนที่ และเวลาทำการของ Happy Paws Clinic',
  },
  eyebrow: 'ติดต่อเรา',
  heading: 'คุยกับเราได้ทุกช่องทาง',
  lead: 'โทร แชท หรือแวะมาที่คลินิก ทีมเคาน์เตอร์ตอบทุกข้อความในเวลาทำการ ถ้าน้องป่วยฉุกเฉิน โทรสายด่วนได้ทันที',

  emergency: {
    title: 'ฉุกเฉิน',
    note: 'อุบัติเหตุ ชัก หายใจลำบาก หรือถูกสัตว์อื่นกัด โทรก่อนออกจากบ้าน ทีมจะเตรียมรับน้องไว้',
    call: 'โทรเลย',
  },

  channelsHeading: 'ช่องทางติดต่อ',
  clinicWide: 'ทั้งคลินิก',
  mapHeading: 'การเดินทาง',
  openMap: 'เปิดใน Google Maps',
  hoursHeading: 'เวลาทำการ',
  hoursUnavailable: 'โทรสอบถามเวลาทำการได้ที่เบอร์คลินิก',
  empty: 'คลินิกยังไม่ได้ใส่ช่องทางติดต่อ',

  /** The action under each kind of entry. */
  actions: {
    phone: 'โทร',
    emergency: 'โทร',
    email: 'ส่งอีเมล',
    line: 'แชทใน LINE',
    facebook: 'เปิด Facebook',
    instagram: 'เปิด Instagram',
    website: 'เปิดเว็บไซต์',
    map: 'เปิดแผนที่',
    other: 'เปิด',
  } satisfies Record<ContactKind, string>,

  cta: {
    heading: 'อยากนัดล่วงหน้า?',
    lead: 'จองคิวออนไลน์ เลือกหมอและเวลาที่สะดวกได้เอง ไม่ต้องรอสาย',
    book: { label: 'จองนัดหมาย', href: '/book' },
  },
};
