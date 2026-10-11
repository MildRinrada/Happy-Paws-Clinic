/** "นัดถัดไป": today's appointments that start soon. */
export const upcomingVisits = {
  heading: (hours: number) => `นัดถัดไป ${hours} ชม.`,
  empty: (hours: number) => `ไม่มีนัดใน ${hours} ชั่วโมงข้างหน้า`,
  unknownPet: 'ไม่ทราบชื่อสัตว์',
  more: (count: number) => `และอีก ${count} นัด`,
  calendar: 'ดูปฏิทินนัดวันนี้',
};
