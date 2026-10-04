import type { DocumentKind } from '../lib/document-rules';

/** Everything the "ส่งไฟล์ให้คลินิก" page says. Edit the wording here. */
export const petDocuments = {
  metaTitle: 'ส่งไฟล์ให้คลินิก',
  title: (name: string) => `ส่งไฟล์ของ${name}ให้คลินิก`,
  intro: 'ส่งรูปอาการ หรือผลตรวจจากคลินิกอื่น ให้ทีมเห็นก่อนน้องมาถึง ไฟล์จะเข้าไปอยู่ในประวัติของน้อง',

  kind: 'ไฟล์นี้คือ',
  kinds: {
    photo: 'รูปอาการ',
    lab_result: 'ผลแล็บ',
    referral: 'ใบส่งตัว',
    imaging: 'ฟิล์ม X-ray หรืออัลตราซาวด์',
    other: 'อื่น ๆ',
  } satisfies Record<DocumentKind, string>,

  titleLabel: 'ชื่อไฟล์ที่คลินิกจะเห็น (ไม่บังคับ)',
  titleHint: 'เช่น ผื่นที่ขาหลัง 3 ต.ค. ถ้าเว้นว่าง ใช้ชื่อไฟล์เดิม',

  file: 'ไฟล์',
  filePick: 'เลือกไฟล์',
  fileChange: 'เปลี่ยนไฟล์',
  fileHint: 'รูป JPG, PNG, WEBP, HEIC หรือไฟล์ PDF ไม่เกิน 10 MB ครั้งละหนึ่งไฟล์',
  fileWrongType: 'ใช้ได้เฉพาะรูป JPG, PNG, WEBP, HEIC หรือไฟล์ PDF',
  fileTooBig: 'ไฟล์ใหญ่เกิน 10 MB',
  fileMissing: 'เลือกไฟล์ก่อนส่ง',

  send: 'ส่งให้คลินิก',
  starting: 'กำลังเตรียมส่ง',
  uploading: 'กำลังอัปโหลด',
  uploadFailed: 'อัปโหลดไม่สำเร็จ ลิงก์อาจหมดอายุหรือไฟล์ไม่ตรงกับที่แจ้ง กรุณาลองใหม่อีกครั้ง',
  unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  back: 'กลับไปหน้าสัตว์เลี้ยง',

  sent: {
    title: 'ส่งไฟล์แล้ว',
    body: (fileName: string) => `ทีมคลินิกจะเห็น ${fileName} ในประวัติของน้อง`,
    another: 'ส่งไฟล์อื่นอีก',
    records: 'ดูประวัติการรักษา',
  },

  linkLabel: 'ส่งรูปหรือเอกสารให้คลินิก',
};
