/**
 * The "ว่างเร็วสุด" block in the hero: the earliest free time to book online, and a button that
 * opens the booking page on that time. The times come from `GET /booking/availability`.
 */
export const nextFreeTime = {
  /**
   * The service whose free times are shown. The first service on the public menu whose `code` is
   * `code` is used; failing that, the first in the `category` category; failing that, the first
   * service the clinic offers online. Change these to feature another service.
   */
  featuredService: { code: 'checkup', category: 'checkup' },

  /** The most branches listed, earliest first, when the clinic has several. */
  maxBranches: 3,

  today: 'วันนี้',
  tomorrow: 'พรุ่งนี้',
  /** "ว่างเร็วสุดวันนี้ 14:30". */
  earliest: (day: string, time: string) => `ว่างเร็วสุด${day} ${time}`,
  /** The line under it: which service the time is for. */
  forService: (name: string) => `สำหรับ${name}`,
  book: 'จองเวลานี้',
  /** Read aloud for the button, which on its own only says "book this time". */
  bookLabel: (day: string, time: string, branch?: string) => `จอง${day} ${time}${branch ? ` ${branch}` : ''}`,

  /** Nothing free today or tomorrow. */
  full: 'วันนี้และพรุ่งนี้คิวเต็มแล้ว',
  seeAll: 'ดูเวลาว่างทั้งหมด',
};
