/**
 * The demo's customer reviews. Written the way owners write them on a clinic's page: short, about
 * the visit, sometimes a complaint. No surnames, phones or pets' names, as the product's rules ask.
 * The name is a placeholder until the first read links each review to a real customer of the
 * customers seed (see `linkReviews`), whose first name then shows instead.
 */

export interface SampleReview {
  name: string;
  anonymous?: boolean;
  rating: number;
  text?: string;
  species: 'สุนัข' | 'แมว';
  service: string;
  days_ago: number;
  status: 'published' | 'pending' | 'rejected';
  reject_reason?: 'personal_data' | 'offensive' | 'off_topic' | 'spam' | 'customer_request';
  reply?: string;
  reply_days_after?: number;
  source?: 'portal' | 'invitation';
}

export const SAMPLE_REVIEWS: SampleReview[] = [
  { name: 'ปิยะนุช', rating: 5, species: 'แมว', service: 'ตรวจสุขภาพประจำปี', days_ago: 1, status: 'pending', text: 'หมอใจเย็นมาก อธิบายผลเลือดให้ฟังทีละค่า น้องแมวไม่เครียดเลยเพราะมีห้องรอแยกจากหมา' },
  { name: 'ธนวัฒน์', rating: 4, species: 'สุนัข', service: 'ฉีดวัคซีน', days_ago: 2, status: 'pending', text: 'บริการดี จองออนไลน์สะดวก แต่วันเสาร์คนเยอะ รอเกินเวลานัดไปประมาณ 20 นาที' },
  { name: 'กมลชนก', rating: 2, species: 'สุนัข', service: 'อาบน้ำตัดขน', days_ago: 3, status: 'pending', text: 'ตัดขนสั้นกว่าที่บอกไว้ค่อนข้างมาก อยากให้ถามย้ำก่อนเริ่มตัด' },
  { name: 'ลูกค้า', anonymous: true, rating: 5, species: 'แมว', service: 'ทำหมัน', days_ago: 4, status: 'pending', text: 'ทำหมันน้องแมว แผลเล็กมาก หมอโทรมาถามอาการวันรุ่งขึ้นด้วย ประทับใจค่ะ' },
  { name: 'สมศักดิ์', rating: 5, species: 'สุนัข', service: 'ฉุกเฉิน', days_ago: 2, status: 'published', text: 'พาน้องมาตอนสองทุ่มเพราะกินช็อกโกแลตเข้าไป หมอรับเคสทันที ทำให้อาเจียนและให้น้ำเกลือ ตอนนี้น้องปกติดีแล้ว ขอบคุณมากครับ', reply: 'ขอบคุณที่ไว้วางใจและรีบพาน้องมานะคะ การมาเร็วช่วยได้มากจริง ๆ ค่ะ', reply_days_after: 1 },
  { name: 'วรรณา', rating: 5, species: 'แมว', service: 'ฉีดวัคซีน', days_ago: 4, status: 'published', text: 'ระบบเตือนวัคซีนทาง LINE ดีมาก ไม่ต้องจำเองเลยว่าถึงรอบเมื่อไร' },
  { name: 'ณัฐพล', rating: 5, species: 'สุนัข', service: 'ตรวจสุขภาพประจำปี', days_ago: 6, status: 'published', text: 'หมอตรวจละเอียด ชั่งน้ำหนัก ฟังหัวใจ ดูฟัน แนะนำเรื่องอาหารสำหรับหมาสูงวัยด้วย ราคาตามที่แจ้งบนเว็บไม่มีบวกเพิ่ม' },
  { name: 'ลูกค้า', anonymous: true, rating: 4, species: 'แมว', service: 'ตรวจเลือด', days_ago: 7, status: 'published', text: 'ผลเลือดได้ภายในชั่วโมงเดียว สะดวกมาก แต่ที่จอดรถมีน้อยไปหน่อย', reply: 'ขอบคุณสำหรับคำแนะนำค่ะ ตอนนี้คลินิกทำสัญญาที่จอดเพิ่มกับอาคารข้าง ๆ แล้ว แจ้งพนักงานเพื่อรับบัตรจอดฟรีได้เลยค่ะ', reply_days_after: 1 },
  { name: 'อรอุมา', rating: 5, species: 'สุนัข', service: 'ขูดหินปูน', days_ago: 9, status: 'published', text: 'ขูดหินปูนแล้วกลิ่นปากหายไปเลย หมอส่งรูปฟันก่อนและหลังให้ดูด้วย' },
  { name: 'ประเสริฐ', rating: 3, species: 'สุนัข', service: 'ฉีดวัคซีน', days_ago: 11, status: 'published', text: 'หมอดี แต่รอคิวนานเกือบชั่วโมงทั้งที่จองมาแล้ว', reply: 'ขออภัยที่ต้องรอนานค่ะ วันนั้นมีเคสฉุกเฉินเข้ามาสองเคสติดกัน ตอนนี้ระบบส่งข้อความแจ้งเมื่อคิวเลื่อนแล้ว จะได้ไม่ต้องนั่งรอที่คลินิกค่ะ', reply_days_after: 1 },
  { name: 'ชลธิชา', rating: 5, species: 'แมว', service: 'ทำหมัน', days_ago: 13, status: 'published', text: 'พนักงานต้อนรับน่ารัก อธิบายการเตรียมตัวก่อนผ่าตัดชัดเจน มีเอกสารให้กลับไปอ่านด้วย' },
  { name: 'ลูกค้า', anonymous: true, rating: 5, species: 'สุนัข', service: 'ตรวจสุขภาพประจำปี', days_ago: 15, status: 'published', text: 'เลี้ยงหมามาสามตัว มาที่นี่ทั้งหมด หมอจำประวัติได้หมดเพราะเปิดดูในระบบได้ทันที' },
  { name: 'จิราพร', rating: 4, species: 'แมว', service: 'ตรวจรักษาทั่วไป', days_ago: 17, status: 'published', text: 'น้องแมวเป็นหวัด หมอให้ยาและนัดดูอาการ หายภายในหนึ่งสัปดาห์ ค่ายาสมเหตุสมผล' },
  { name: 'วีระ', rating: 5, species: 'สุนัข', service: 'ผ่าตัด', days_ago: 19, status: 'published', text: 'น้องกินของเล่นเข้าไปต้องผ่าตัด หมออัปเดตอาการทาง LINE ตลอดระหว่างนอนโรงพยาบาล', reply: 'ดีใจที่น้องกลับบ้านได้แล้วนะคะ อย่าลืมพามาตัดไหมตามนัดค่ะ', reply_days_after: 2 },
  { name: 'สุภาวดี', rating: 5, species: 'แมว', service: 'ฉีดวัคซีน', days_ago: 22, status: 'published' },
  { name: 'ลูกค้า', anonymous: true, rating: 1, species: 'สุนัข', service: 'ตรวจรักษาทั่วไป', days_ago: 24, status: 'rejected', reject_reason: 'personal_data', text: 'พนักงานชื่อ ... เบอร์ 08x-xxx-xxxx พูดจาไม่ดี' },
  { name: 'อนุชา', rating: 4, species: 'สุนัข', service: 'อาบน้ำตัดขน', days_ago: 25, status: 'published', text: 'ตัดขนสวย อาบน้ำหอม แต่ควรจองล่วงหน้าหลายวันเพราะคิวเต็มเร็ว' },
  { name: 'พิมพ์ชนก', rating: 5, species: 'แมว', service: 'ตรวจสุขภาพประจำปี', days_ago: 28, status: 'published', text: 'แมวขี้กลัวมาก หมอให้ตรวจในกระเป๋าเลยไม่ต้องดึงออกมา ประทับใจที่ใส่ใจความรู้สึกของน้อง' },
  { name: 'ลูกค้า', anonymous: true, rating: 5, species: 'สุนัข', service: 'ฉีดวัคซีน', days_ago: 31, status: 'published', text: 'สะอาด ไม่มีกลิ่น ห้องตรวจกว้าง' },
  { name: 'ศิริลักษณ์', rating: 5, species: 'สุนัข', service: 'ตรวจเลือด', days_ago: 34, status: 'published', text: 'หมอโทรมาอธิบายผลเลือดเองหลังได้ผล ไม่ต้องกลับมาที่คลินิกอีกรอบ', reply: 'ขอบคุณค่ะ ถ้ามีคำถามเรื่องผลตรวจเพิ่มเติม ทักทาง LINE ของคลินิกได้ตลอดค่ะ', reply_days_after: 1 },
  { name: 'ธีรพงษ์', rating: 3, species: 'แมว', service: 'ตรวจรักษาทั่วไป', days_ago: 38, status: 'published', text: 'รักษาดี แต่ค่ารักษาสูงกว่าที่คาดไว้ อยากให้แจ้งราคาประเมินก่อนเริ่มตรวจเพิ่มเติม', reply: 'ขอบคุณสำหรับคำติชมค่ะ ตอนนี้หมอจะแจ้งประมาณการค่าใช้จ่ายและขอความเห็นชอบก่อนตรวจเพิ่มทุกครั้งค่ะ', reply_days_after: 2 },
  { name: 'มาลัย', rating: 5, species: 'แมว', service: 'ทำหมัน', days_ago: 41, status: 'published' },
  { name: 'ลูกค้า', anonymous: true, rating: 2, species: 'สุนัข', service: 'สินค้าและอาหาร', days_ago: 43, status: 'rejected', reject_reason: 'spam', text: 'สั่งอาหารหมาราคาถูกได้ที่เพจของเรา ส่งฟรีทั่วประเทศ' },
  { name: 'เกียรติศักดิ์', rating: 5, species: 'สุนัข', service: 'ตรวจสุขภาพประจำปี', days_ago: 45, status: 'published', text: 'จองคิวผ่านเว็บได้ตอนดึก ๆ สะดวกสำหรับคนทำงาน' },
  { name: 'นันทนา', rating: 4, species: 'แมว', service: 'ฉีดวัคซีน', days_ago: 49, status: 'published', text: 'หมอและพยาบาลอ่อนโยนกับน้อง ห้องรอแมวเงียบดี' },
  { name: 'ลูกค้า', anonymous: true, rating: 5, species: 'สุนัข', service: 'ฉุกเฉิน', days_ago: 53, status: 'published', text: 'โทรมาตอนน้องชักกลางดึก พนักงานแนะนำวิธีดูแลระหว่างเดินทางและเตรียมห้องรอไว้ ขอบคุณมากค่ะ', reply: 'ขอบคุณที่ไว้ใจค่ะ ขอให้น้องแข็งแรงนะคะ', reply_days_after: 1 },
  { name: 'ปรีชา', rating: 5, species: 'สุนัข', service: 'ขูดหินปูน', days_ago: 57, status: 'published', text: 'ตรวจเลือดก่อนดมยาให้ด้วย หมาอายุ 11 ปีแล้วแต่ทำได้ปลอดภัย' },
  { name: 'รัตนา', rating: 5, species: 'แมว', service: 'ตรวจสุขภาพประจำปี', days_ago: 62, status: 'published', text: 'ใบสรุปการตรวจดูในแอปได้เลย เก็บไว้เทียบปีหน้าได้ง่าย' },
  { name: 'สุทธิพงษ์', rating: 4, species: 'สุนัข', service: 'ตรวจรักษาทั่วไป', days_ago: 66, status: 'published', text: 'หมอให้คำแนะนำเรื่องโรคผิวหนังละเอียด ใช้ยาตามที่สั่งแล้วดีขึ้นมาก' },
  { name: 'ลูกค้า', anonymous: true, rating: 5, species: 'แมว', service: 'ฉีดวัคซีน', days_ago: 71, status: 'published' },
  { name: 'อัญชลี', rating: 5, species: 'สุนัข', service: 'ทำหมัน', days_ago: 76, status: 'published', text: 'ดูแลหลังผ่าตัดดีมาก ให้ปลอกคอและยากลับบ้านพร้อมใบอธิบายการดูแลแผล', reply: 'ขอบคุณค่ะ ถ้าแผลมีอะไรผิดปกติ ส่งรูปมาทาง LINE ได้เลยนะคะ', reply_days_after: 1 },
  { name: 'ภานุวัฒน์', rating: 5, species: 'แมว', service: 'ตรวจเลือด', days_ago: 82, status: 'published', text: 'แมวเป็นโรคไต หมอวางแผนตรวจติดตามทุกสามเดือน มีระบบเตือนนัดให้ด้วย' },
];

/** Review links the demo seeds, so the invitation page can be tried in each of its states. */
export const SAMPLE_INVITATIONS = [
  { token: 'hprv_demo-usable-7Kq2mXw9', state: 'usable', days_ago: 3, service: 'ฉีดวัคซีน', pet_name: 'ข้าวปั้น' },
  { token: 'hprv_demo-used-4Hn8pRt2', state: 'used', days_ago: 6, service: 'ตรวจสุขภาพประจำปี', pet_name: 'ถุงทอง' },
  { token: 'hprv_demo-expired-9Wz3cLb5', state: 'expired', days_ago: 40, service: 'ขูดหินปูน', pet_name: 'ลัคกี้' },
] as const;
