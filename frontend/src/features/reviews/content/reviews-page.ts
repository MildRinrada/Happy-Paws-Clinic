/** The public `/reviews` page and the review cards on the site. */
export const reviewsPage = {
  metaTitle: 'รีวิวจากลูกค้าจริง | Happy Paws Clinic',
  metaDescription: 'รีวิวจากเจ้าของสุนัขและแมวที่มาใช้บริการจริง คลินิกตรวจทุกรีวิวก่อนขึ้นเว็บ และไม่ลบรีวิวเพราะคะแนนต่ำ',
  title: 'รีวิวจากลูกค้าจริง',
  intro: 'ทุกรีวิวมาจากลูกค้าที่มาใช้บริการจริง คลินิกอ่านก่อนขึ้นหน้าเว็บ และนำออกเฉพาะรีวิวที่มีข้อมูลส่วนบุคคล ภาษาไม่สุภาพ ไม่เกี่ยวกับบริการ สแปม หรือเมื่อเจ้าของรีวิวขอ ไม่นำออกเพราะคะแนนต่ำ',

  empty: 'ยังไม่มีรีวิวที่เผยแพร่',
  unavailable: 'โหลดรีวิวไม่ได้ในขณะนี้ ลองใหม่อีกครั้ง',

  average: 'คะแนนเฉลี่ย',
  outOf: 'จาก 5',
  count: (count: number) => `จาก ${count.toLocaleString('th-TH')} รีวิว`,
  stars: (stars: number) => `${stars} ดาว`,
  barLabel: (stars: number, count: number) => `${stars} ดาว ${count.toLocaleString('th-TH')} รีวิว`,
  ratingLabel: (rating: number) => `${rating} จาก 5 ดาว`,

  owner: (species?: string | null) => (species ? `เจ้าของ${species}` : 'เจ้าของสัตว์เลี้ยง'),
  reply: 'คลินิกตอบ',

  next: 'รีวิวก่อนหน้านี้',
  first: 'กลับไปรีวิวล่าสุด',

  /** The link under the home page's review column. */
  more: 'ดูรีวิวทั้งหมด',
};
