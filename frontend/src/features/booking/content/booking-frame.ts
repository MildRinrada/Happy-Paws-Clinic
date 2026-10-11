/**
 * The words around the booking steps on `/book` and `/portal/book`: the page's head, the progress
 * bar, the summary card at the right, the notes under it, and the day and time picker. The words
 * inside each step stay in `booking-page.ts` and `guest-booking-page.ts`. Edit the wording here.
 */
export const bookingFrame = {
  head: {
    breadcrumbLabel: 'ตำแหน่งของหน้านี้',
    backToServices: 'กลับไปหน้าบริการ',
  },

  progress: {
    label: 'ขั้นตอนการจอง',
    /** On a phone, where the step names do not fit under the bars. */
    stepOf: (current: number, total: number) => `ขั้นที่ ${current} จาก ${total}`,
    edit: 'แก้ไข',
    editLabel: (step: string) => `แก้ไขขั้น ${step}`,
  },

  next: {
    time: 'ถัดไป เลือกวันและเวลา',
    phone: 'ถัดไป ยืนยันด้วยเบอร์โทร',
    confirm: 'ถัดไป ตรวจสอบก่อนยืนยัน',
  },

  details: {
    title: 'รายละเอียดการจอง',
  },

  time: {
    title: 'เลือกวันและเวลา',
    day: 'วัน',
    today: 'วันนี้',
    free: 'ว่าง',
    full: 'เต็ม',
    previousWeek: 'สัปดาห์ก่อน',
    nextWeek: 'สัปดาห์ถัดไป',
    slots: 'เวลาที่ว่าง',
    /**
     * The note under the times in the design, for times already booked drawn struck through.
     * Not shown yet: `GET /booking/availability` lists only the free times, so the page cannot
     * tell a booked time from one outside the vets' hours. Shown once the API says which are taken.
     */
    takenNote: 'เวลาที่ขีดฆ่ามีคนจองแล้ว',
    vet: 'สัตวแพทย์',
    anyVet: 'สัตวแพทย์ที่ว่างในวันนั้น',
    back: 'ย้อนกลับ เลือกบริการ',
    dayLabel: (date: string, status: string) => (status ? `${date} ${status}` : date),
  },

  summary: {
    title: 'สรุปการจอง',
    pet: 'น้อง',
    service: 'บริการ',
    type: 'ประเภทนัด',
    when: 'วันและเวลา',
    vet: 'สัตวแพทย์',
    branch: 'สาขา',
    reason: 'อาการที่แจ้ง',
    change: 'เปลี่ยน',
    changeLabel: 'เปลี่ยนบริการ',
    notChosen: 'ยังไม่ได้เลือก',
    noTime: 'ยังไม่ได้เลือกเวลา',
    at: (day: string, time: string) => `${day} เวลา ${time}`,
    anyVet: 'สัตวแพทย์ที่ว่างในวันนั้น',
  },

  holdNote: {
    guest: 'เราจะกันเวลานี้ไว้ให้ 10 นาทีระหว่างที่คุณกรอกรหัสยืนยันจาก SMS',
    portal: 'กดถัดไปแล้ว เราจะกันเวลานี้ไว้ให้ 10 นาทีระหว่างที่คุณตรวจสอบก่อนยืนยัน',
  },

  questions: {
    title: 'มีคำถามก่อนจอง?',
    links: [
      { label: 'ดูช่องทางติดต่อ', href: '/contact' },
      { label: 'รู้จักทีมสัตวแพทย์', href: '/team' },
    ],
  },
};
