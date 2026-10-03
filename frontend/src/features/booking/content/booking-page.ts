import type { BookableType } from '../types/booking.types';

/** Everything the booking page says. Edit the wording here. */
export const bookingPage = {
  title: 'จองนัด',
  intro: 'เลือกน้อง บริการ และเวลาที่สะดวก ใช้เวลาไม่ถึงนาที',

  steps: ['รายละเอียด', 'วันและเวลา', 'ยืนยัน'],

  details: {
    pet: 'นัดให้น้องตัวไหน',
    noPets: 'ยังไม่มีสัตว์เลี้ยงในบัญชีที่จองนัดได้ ติดต่อคลินิกเพื่อเพิ่มน้องเข้าบัญชีนี้',
    branch: 'สาขา',
    service: 'บริการ',
    noServices: 'สาขานี้ยังไม่มีบริการที่เปิดให้จองออนไลน์ โทรจองกับคลินิกได้เลย',
    minutes: (count: number) => `${count} นาที`,
    priceFrom: (price: string) => `เริ่มต้น ${price}`,
    type: 'ประเภทนัด',
    types: {
      regular: { label: 'นัดปกติ', hint: 'ตรวจ ฉีดวัคซีน หรือปรึกษาเรื่องใหม่' },
      follow_up: { label: 'ติดตามผล', hint: 'กลับมาตามที่หมอนัดหลังการรักษา' },
    } satisfies Record<BookableType, { label: string; hint: string }>,
    reason: 'อาการหรือสิ่งที่อยากให้หมอรู้ (ไม่บังคับ)',
    reasonPlaceholder: 'เช่น เกาหูบ่อย 3 วันแล้ว',
    next: 'เลือกวันและเวลา',
  },

  time: {
    day: 'เลือกวัน',
    otherDay: 'วันอื่น',
    vet: 'สัตวแพทย์',
    anyVet: 'คนไหนก็ได้',
    slots: 'เวลาที่ว่าง',
    loading: 'กำลังดูเวลาที่ว่าง',
    none: 'วันนี้ไม่มีเวลาว่างสำหรับบริการนี้ ลองเลือกวันอื่น',
    holding: 'กำลังกันเวลาไว้ให้',
    back: 'ย้อนกลับ',
  },

  confirm: {
    title: 'ตรวจสอบก่อนยืนยัน',
    pet: 'น้อง',
    service: 'บริการ',
    type: 'ประเภท',
    when: 'วันและเวลา',
    vet: 'สัตวแพทย์',
    branch: 'สาขา',
    reason: 'อาการ',
    held: (time: string) => `กันเวลานี้ไว้ให้ถึง ${time}`,
    expired: 'หมดเวลาที่กันไว้ กรุณาเลือกเวลาใหม่',
    submit: 'ยืนยันการจอง',
    submitting: 'กำลังจอง',
    back: 'เลือกเวลาใหม่',
  },

  done: {
    title: 'จองนัดเรียบร้อย',
    body: 'คลินิกจะยืนยันนัดอีกครั้ง ดูสถานะได้ที่หน้านัดหมาย',
    appointments: 'ดูนัดหมายของฉัน',
    home: 'กลับหน้าแรก',
  },

  suggested: 'เวลาใกล้เคียงที่ยังว่าง',
  unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  vetFallback: 'สัตวแพทย์ที่ว่างในเวลานั้น',
};
