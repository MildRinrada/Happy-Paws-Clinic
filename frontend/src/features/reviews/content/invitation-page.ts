/** The public `/review/{token}` page a review invitation links to. */
export const invitationPage = {
  metaTitle: 'รีวิวการมาใช้บริการ | Happy Paws Clinic',
  title: 'รีวิวการมาใช้บริการ',
  visitOn: (day: string) => `การมาเมื่อ ${day}`,
  service: 'บริการ',
  pet: 'สัตว์เลี้ยง',
  expiresOn: (day: string) => `ลิงก์นี้ใช้ได้ถึง ${day}`,

  usedTitle: 'ลิงก์นี้ใช้ไปแล้ว',
  usedBody: 'การมาครั้งนี้ได้รับรีวิวแล้ว ขอบคุณที่สละเวลาเล่าให้คลินิกฟัง',
  expiredTitle: 'ลิงก์นี้หมดอายุแล้ว',
  expiredBody: 'ลิงก์รีวิวใช้ได้ 30 วันหลังได้รับข้อความ ถ้ายังอยากเล่าให้คลินิกฟัง ติดต่อคลินิกได้เลย',
  notFoundTitle: 'ไม่พบลิงก์รีวิวนี้',
  notFoundBody: 'ลิงก์อาจพิมพ์ไม่ครบ ลองเปิดจากข้อความที่ได้รับอีกครั้ง',
  unavailable: 'ติดต่อระบบไม่ได้ในขณะนี้ ลองใหม่อีกครั้ง',

  seeReviews: 'ดูรีวิวจากลูกค้าคนอื่น',
  home: 'กลับหน้าแรก',
};
