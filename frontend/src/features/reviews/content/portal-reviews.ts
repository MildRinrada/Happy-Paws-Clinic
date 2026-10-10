/** The portal's review page and its card on the portal home. */
export const portalReviews = {
  metaTitle: 'รีวิวการมาใช้บริการ | Happy Paws Clinic',
  title: 'รีวิวการมาใช้บริการ',
  intro: 'การมาใช้บริการใน 60 วันที่ผ่านมาที่ยังไม่ได้รีวิว รีวิวได้ครั้งละหนึ่งการมา',
  empty: 'ยังไม่มีการมาใช้บริการที่รอรีวิว',
  visitTitle: (pet: string, day: string) => `${pet} เมื่อ ${day}`,
  until: (day: string) => `รีวิวได้ถึง ${day}`,
  open: 'เขียนรีวิว',

  /** The card on the portal home. */
  card: {
    title: 'รีวิวการมาครั้งล่าสุด',
    body: (pet: string, day: string) => `ช่วยเล่าให้คลินิกฟังหน่อย ว่าการพา ${pet} มาเมื่อ ${day} เป็นอย่างไร`,
    more: (count: number) => (count > 1 ? `รอรีวิวอีก ${count - 1} ครั้ง` : ''),
    action: 'เขียนรีวิว',
  },
};
