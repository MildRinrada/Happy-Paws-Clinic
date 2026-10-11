/**
 * Everything the public "บทความ" page says that does not come from the API. The articles
 * themselves are the clinic's own, written by staff under ตั้งค่า > บทความ.
 */

export const articlesMeta = {
  title: 'บทความจากทีมสัตวแพทย์ | Happy Paws Clinic',
  description: 'บทความสุขภาพสุนัขและแมวจากทีมสัตวแพทย์ อ่านก่อนพาน้องมาคลินิก',
};

/** The words at the top of the page. */
export const articlesHeading = {
  eyebrow: 'บทความ',
  heading: 'บทความจากทีมสัตวแพทย์',
  lead: 'เรื่องที่เจ้าของถามบ่อยในห้องตรวจ เขียนให้อ่านเข้าใจง่ายโดยหมอของคลินิก',
};

export const articleSearch = {
  /** Read out for the search box, and shown inside it while it is empty. */
  label: 'ค้นหาบทความ',
  /** Read out for the magnifier, which sends the search. */
  submit: 'ค้นหา',
  /** Read out for the cross that clears the search. */
  clear: 'ล้างคำค้น',
  /** Over the results. */
  results: (q: string, count: number) => `ผลการค้นหา “${q}” พบ ${count} บทความ`,
  /** Nothing matched. */
  noMatch: (q: string) => `ไม่พบบทความที่ตรงกับ “${q}”`,
  noMatchHint: 'ลองใช้คำที่สั้นลง หรือ',
};

export const articleTopics = {
  /** Read out for the row of topic chips. */
  label: 'หัวข้อบทความ',
  all: 'ทั้งหมด',
  /** A topic with no articles in it. */
  empty: (tag: string) => `ยังไม่มีบทความในหัวข้อ “${tag}”`,
};

export const articleCards = {
  /** On the large card at the top: the newest article. */
  latest: 'บทความล่าสุด',
  read: 'อ่านบทความ',
  /** In the frame of an article with no cover yet. */
  coverLabel: 'รูปบทความ',
  /** Before the author's name. */
  by: 'โดย',
};

export const articlesList = {
  /** The API could not be reached. */
  unavailable: 'ยังโหลดบทความไม่ได้ ลองใหม่อีกครั้งภายหลัง',
  /** Nothing has been published yet. */
  empty: 'ยังไม่มีบทความ',
  /** Back to every article, from an empty search or topic. */
  showAll: 'ดูบทความทั้งหมด',
  /** Under the grid, when there are older articles on another page. */
  older: 'บทความก่อนหน้านี้',
};

/** The grey band that closes the page. */
export const articlesCta = {
  heading: 'อ่านแล้วยังกังวลเรื่องน้อง?',
  lead: 'นัดคุยกับสัตวแพทย์ได้เลย หรือทักแชทถามเคาน์เตอร์ก่อนก็ได้',
  book: { label: 'จองนัดหมาย', href: '/book' },
  contact: { label: 'ติดต่อคลินิก', href: '/contact' },
};
