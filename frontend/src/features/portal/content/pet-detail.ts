/** Everything one pet's page says, beyond its profile card. Edit the wording here. */
export const petDetail = {
  back: 'กลับไปรายชื่อสัตว์เลี้ยง',

  switcher: {
    label: 'เลือกสัตว์เลี้ยง',
    /** The control that holds every pet when they do not all fit as chips. */
    all: (count: number) => `ทั้งหมด ${count} ตัว`,
    searchLabel: 'ค้นหาน้อง',
    searchPlaceholder: 'พิมพ์ชื่อหรือชื่อเล่น',
    noMatch: 'ไม่พบน้องที่ชื่อนี้',
    deceasedGroup: 'น้องที่จากไปแล้ว',
    deceasedMark: '(จากไปแล้ว)',
  },

  stats: {
    weight: 'น้ำหนักล่าสุด',
    weightValue: (kg: number) => `${kg} กก.`,
    noWeight: 'ยังไม่มีบันทึก',
    vaccineOverdue: 'วัคซีนเลยกำหนด',
    vaccineDueSoon: 'วัคซีนใกล้ครบกำหนด',
    vaccineClear: 'วัคซีน',
    vaccineNoneDue: 'ยังไม่ถึงกำหนด',
    /** "พิษสุนัขบ้า +1" when more than one vaccine is due. */
    moreVaccines: (name: string, count: number) => `${name} +${count}`,
    bookVaccine: 'จองนัดฉีด',
    nextVisit: 'นัดครั้งถัดไป',
    noNextVisit: 'ยังไม่มีนัด',
    /** "14:30 น. ฉีดวัคซีน" under the date of the next appointment. */
    nextVisitNote: (time: string, service?: string) => (service ? `${time} น. ${service}` : `${time} น.`),
    book: 'จองนัด',
    seeAppointments: 'ดูนัดหมาย',
  },

  weight: {
    historyTitle: 'น้ำหนักย้อนหลัง',
    latestTitle: 'น้ำหนักล่าสุด',
    unit: 'กก.',
    count: (count: number) => `${count} ครั้งล่าสุด`,
    noWeight: 'ยังไม่มีบันทึกน้ำหนัก',
    /** What a screen reader hears in place of the bars. */
    summary: (count: number) => `น้ำหนัก ${count} ครั้งล่าสุด จากเก่าไปใหม่`,
    point: (date: string, kg: number) => `${date} ${kg} กก.`,
  },

  vaccines: {
    title: 'วัคซีน',
    overdue: 'เลยกำหนด',
    dueSoon: 'ใกล้ครบกำหนด',
    given: 'ฉีดแล้ว',
    dueOn: (date: string) => `ครบกำหนด ${date}`,
    givenOn: (date: string) => `ฉีดแล้ว ${date}`,
    empty: 'ไม่มีวัคซีนที่เลยกำหนดหรือใกล้ครบกำหนด',
    /** Until the clinic's full vaccine list can be read here (ส่วนขยายข้อ 44, pending approval). */
    partial: 'ตอนนี้แสดงเฉพาะวัคซีนที่ใกล้ครบกำหนด ประวัติวัคซีนที่ฉีดแล้วจะขึ้นที่นี่ในภายหลัง',
    book: (vaccine: string) => `จองนัดฉีด${vaccine}`,
  },

  visits: {
    title: 'มาตรวจล่าสุด',
    history: 'ดูประวัติการรักษา',
    empty: 'ยังไม่มีการมาตรวจที่บันทึกไว้ในบัญชีนี้',
    serviceFallback: 'มาตรวจ',
    /** "มาตรวจ 14 มี.ค. 2569 สพ.ญ. มินตรา". */
    line: (date: string, vet?: string) => (vet ? `มาตรวจ ${date} ${vet}` : `มาตรวจ ${date}`),
  },
};
