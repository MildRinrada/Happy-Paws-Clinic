/**
 * "เวลารอ walk-in ตอนนี้": about how long someone who walks in without an appointment waits now,
 * from `GET /queue/wait-estimate`. Shown on the home page hero, the contact page and the emergency
 * page. The figure is a rough guide, worked out from the queue and the vets on shift.
 */
export const waitEstimate = {
  heading: 'เวลารอ walk-in ตอนนี้',

  /** The most branches listed when the clinic has several. */
  maxBranches: 5,

  /** "ตอนนี้รอประมาณ 20 ถึง 30 นาที (3 คิว)". */
  waiting: (low: number, high: number, queue: number) => `ตอนนี้รอประมาณ ${low} ถึง ${high} นาที (${queue} คิว)`,
  /** Nobody waiting. */
  noWait: 'ตอนนี้ไม่มีคิวรอ เข้ามาได้เลย',
  closed: 'ตอนนี้ปิดอยู่',
  /** After "ตอนนี้ปิดอยู่" when the next opening is known: "เปิดอีกครั้งพรุ่งนี้ 09:00", as the emergency page says it. */
  opensAgain: (day: string, time: string) => `เปิดอีกครั้ง${day} ${time}`,
  today: 'วันนี้',
  tomorrow: 'พรุ่งนี้',
  /** A later day: "วันจันทร์ที่ 12 ต.ค.". */
  laterDay: (weekday: string, date: string) => `วัน${weekday}ที่ ${date}`,
  /** Under the lines: the figure is a guess, and emergencies go first. */
  note: 'เป็นการประมาณจากคิวและหมอที่เข้าเวรอยู่ เคสฉุกเฉินได้ตรวจก่อนเสมอ',
};
