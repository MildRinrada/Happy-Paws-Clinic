/**
 * Everything the emergency page (`/emergency`) says apart from the medical advice, which is in
 * `emergency-care.ts` for the clinic vet to review. The numbers, hours, closures and maps come from
 * the API: the clinic's contact entries and opening hours, edited in the staff console.
 */
export const emergencyPage = {
  meta: {
    title: 'กรณีฉุกเฉิน สัตว์เลี้ยงป่วยหนักหรือบาดเจ็บ | Happy Paws Clinic',
    description:
      'เบอร์โทรฉุกเฉินของ Happy Paws Clinic คลินิกเปิดอยู่ไหมตอนนี้ อาการที่ต้องพาไปหาหมอทันที สิ่งที่ควรทำและไม่ควรทำระหว่างทาง และเส้นทางมาคลินิก',
  },

  hero: {
    eyebrow: 'กรณีฉุกเฉิน',
    heading: 'น้องป่วยหนักหรือบาดเจ็บ โทรหาเราก่อนออกจากบ้าน',
    lead: 'บอกอาการทางโทรศัพท์ ทีมจะบอกว่าต้องทำอะไรระหว่างทาง และเตรียมรับน้องไว้ให้',
  },

  call: {
    emergency: 'สายด่วนฉุกเฉิน',
    fallback: 'เบอร์คลินิก',
    action: 'แตะเพื่อโทร',
    /** Read aloud for the call button. */
    callLabel: (label: string, value: string) => `โทร ${label} ${value}`,
    none: 'ยังไม่มีเบอร์โทรในระบบ ดูช่องทางอื่นได้ที่หน้าติดต่อเรา',
    contact: { label: 'ไปหน้าติดต่อเรา', href: '/contact' },
  },

  status: {
    heading: 'ตอนนี้คลินิกเปิดอยู่ไหม',
    open: 'เปิดอยู่',
    closed: 'ปิดอยู่',
    closesAt: (time: string) => `เปิดถึง ${time} วันนี้`,
    opensAt: (day: string, time: string) => `เปิดอีกครั้ง${day} ${time}`,
    today: 'วันนี้',
    tomorrow: 'พรุ่งนี้',
    /** A later day: "วันจันทร์ที่ 13 ต.ค.". */
    laterDay: (weekday: string, date: string) => `วัน${weekday}ที่ ${date}`,
    holiday: (name: string) => `วันนี้หยุด ${name}`,
    noOpening: 'ยังไม่มีวันเปิดในสองสัปดาห์ข้างหน้า โทรสอบถามคลินิก',
    unknown: 'ยังไม่มีเวลาทำการในระบบ โทรสอบถามคลินิกได้เลย',
    afterHours: 'นอกเวลาทำการ โทรเบอร์ด้านบนก่อนออกจากบ้านเสมอ เพื่อถามว่าควรพาน้องไปที่ไหน',
    checkedAt: (time: string) => `ข้อมูล ณ เวลา ${time} น. ตามเวลาของคลินิก`,
  },

  signs: {
    heading: 'อาการแบบนี้ ต้องพาไปหาหมอทันที',
    lead: 'ไม่ต้องรอนัด ไม่ต้องรอดูอาการ โทรหาคลินิกแล้วออกเดินทางได้เลย',
  },

  care: {
    doHeading: 'ระหว่างทางควรทำ',
    dontHeading: 'สิ่งที่ไม่ควรทำ',
  },

  map: {
    heading: 'เส้นทางมาคลินิก',
    more: { label: 'ช่องทางติดต่อทั้งหมด', href: '/contact' },
  },

  note: {
    heading: 'ข้อมูลในหน้านี้เป็นคำแนะนำเบื้องต้น ไม่ใช่การวินิจฉัย',
    body: 'อาการของน้องแต่ละตัวต่างกัน ถ้าไม่แน่ใจว่าฉุกเฉินหรือไม่ โทรถามคลินิกได้เสมอ',
    reviewed: 'ตรวจทานเนื้อหาโดย {name} เมื่อ {date}',
    pendingReview: 'เนื้อหาในหน้านี้อยู่ระหว่างรอสัตวแพทย์ของคลินิกตรวจทาน',
  },
};
