import type { BadgeTone } from '@/components/ui/badge';

/** The console's words for the clinic's privacy notice (docs/contract-extensions.md § 35). */
export const staffPolicyText = {
  metaTitle: 'ประกาศความเป็นส่วนตัว | ตั้งค่า',
  title: 'ประกาศความเป็นส่วนตัว',
  subtitle: 'คลินิกเป็นผู้ควบคุมข้อมูลตาม PDPA จึงเป็นผู้เขียนและเผยแพร่ประกาศนี้เอง ฉบับที่เผยแพร่แล้วแก้ไม่ได้ ถ้าจะเปลี่ยนให้สร้างร่างใหม่',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูประกาศความเป็นส่วนตัว (ต้องมี settings:read)',
  viewPublic: 'ดูหน้าสาธารณะ',
  back: 'กลับไปรายการฉบับ',
  missing: 'ไม่พบฉบับนี้ อาจถูกเปิดจากลิงก์เก่า',
};

export const notPublishedWarning = {
  title: 'คลินิกยังไม่ได้เผยแพร่ประกาศความเป็นส่วนตัว',
  body: 'หน้า /privacy ตอนนี้บอกลูกค้าว่าคลินิกยังไม่ได้เผยแพร่ประกาศ และความยินยอมที่บันทึกระหว่างนี้ไม่มีเลขฉบับ ตรวจร่างให้ครบ ให้นักกฎหมายอ่าน แล้วให้เจ้าของคลินิกกดเผยแพร่',
  ownerOnly: 'เฉพาะเจ้าของคลินิก (สิทธิ์ settings:danger) ที่เผยแพร่ได้',
};

export const dashboardNotice = {
  text: 'คลินิกยังไม่ได้เผยแพร่ประกาศความเป็นส่วนตัว',
  link: 'ไปที่หน้าประกาศ',
};

export const newerTemplateNotice = {
  title: 'มี template ฉบับใหม่',
  body: (version: string) =>
    `ระบบรุ่นนี้มี template ประกาศความเป็นส่วนตัวฉบับ ${version} ซึ่งเขียนจากสิ่งที่ระบบทำในรุ่นนี้ ระบบไม่เปลี่ยนประกาศของคลินิกเอง ถ้าต้องการ ให้สร้างร่างจาก template นี้แล้วเทียบกับฉบับปัจจุบัน`,
};

export const createDraftText = {
  fromCurrent: 'สร้างร่างจากฉบับปัจจุบัน',
  fromTemplate: 'สร้างร่างจาก template',
  fromThis: 'คัดลอกเป็นร่างใหม่',
  creating: 'กำลังสร้างร่าง…',
};

export const policyStatusLabels: Record<'draft' | 'published' | 'current', { text: string; tone: BadgeTone }> = {
  draft: { text: 'ร่าง', tone: 'neutral' },
  published: { text: 'เผยแพร่แล้ว', tone: 'blue' },
  current: { text: 'ฉบับปัจจุบัน', tone: 'green' },
};

export const versionListText = {
  heading: 'ทุกฉบับ',
  empty: 'ยังไม่มีฉบับใด',
  draftTitle: 'ร่าง',
  publishedTitle: (label: string) => `ฉบับ ${label}`,
  fromTemplate: (version: string) => `ทำจาก template ${version}`,
  fromVersion: 'คัดลอกจากฉบับอื่น',
  createdAt: (date: string) => `สร้าง ${date}`,
  updatedAt: (date: string) => `แก้ล่าสุด ${date}`,
  publishedAt: (date: string) => `เผยแพร่ ${date}`,
  effective: (date: string) => `มีผลตั้งแต่ ${date}`,
  sections: (count: number) => `${count} หัวข้อ`,
  edit: 'แก้ร่าง',
  open: 'เปิดดู',
};

export const templateDraftWarning = {
  title: 'ร่างนี้ทำจาก template ที่ผู้พัฒนาระบบเขียน',
  body: 'template เขียนจากสิ่งที่ระบบทำจริง แต่ไม่ใช่คำแนะนำทางกฎหมาย คลินิกต้องตรวจทุกข้อให้ตรงกับการทำงานจริงของคลินิก และควรให้ทนายความหรือที่ปรึกษาด้านกฎหมายคุ้มครองข้อมูลส่วนบุคคลตรวจก่อนเผยแพร่',
};

export const editorText = {
  heading: 'แก้ร่าง',
  sectionHeading: (index: number) => `หัวข้อที่ ${index + 1}`,
  fields: {
    heading: 'ชื่อหัวข้อ',
    anchor: 'ชื่อจุดเชื่อม',
    anchorHint: 'ใช้ในลิงก์ เช่น /privacy#cookies ตัวพิมพ์เล็กภาษาอังกฤษ ตัวเลข และขีดกลาง',
    body: 'เนื้อหา',
    count: (length: number) => `${length.toLocaleString('th-TH')} / 10,000 ตัวอักษร`,
  },
  moveUp: 'เลื่อนขึ้น',
  moveDown: 'เลื่อนลง',
  remove: 'ลบหัวข้อนี้',
  add: 'เพิ่มหัวข้อ',
  newHeading: 'หัวข้อใหม่',
  save: 'บันทึกร่าง',
  saving: 'กำลังบันทึก…',
  saved: 'บันทึกร่างแล้ว ยังไม่มีใครเห็นนอกจากพนักงาน',
  unsaved: 'มีการแก้ที่ยังไม่บันทึก',
  sectionsError: 'ตรวจหัวข้อที่มีข้อความสีแดง',
};

export const formatHelp = {
  title: 'วิธีเขียนเนื้อหา',
  rules: [
    'เว้นบรรทัดว่างหนึ่งบรรทัดเพื่อขึ้นย่อหน้าใหม่',
    'บรรทัดที่ขึ้นต้นด้วย "- " (ขีดกลางตามด้วยเว้นวรรค) เป็นรายการ',
    'ลิงก์ไปหน้าในเว็บของคลินิกเขียนว่า [ข้อความ](/portal/account) ย่อหน้าที่มีแค่ลิงก์เดียวแสดงเป็นปุ่ม',
    'ห้ามใส่ HTML และห้ามลิงก์ออกนอกเว็บของคลินิก ที่อยู่เว็บอื่นพิมพ์เป็นข้อความธรรมดาได้',
  ],
};

export const previewText = {
  heading: 'ตัวอย่างหน้าสาธารณะ',
  note: 'แสดงแบบเดียวกับหน้า /privacy หัวข้อแรกเรื่องข้อมูลคลินิกมาจากหน้าตั้งค่าคลินิกและไม่แสดงในตัวอย่างนี้',
  empty: 'ยังไม่มีเนื้อหา',
};

export const publishText = {
  heading: 'เผยแพร่',
  intro: 'เมื่อเผยแพร่ ร่างนี้จะเป็นประกาศของคลินิกทันที หน้า /privacy แสดงฉบับนี้ และทุกความยินยอมที่บันทึกต่อจากนี้มีเลขฉบับนี้กำกับ ฉบับที่เผยแพร่แล้วแก้หรือลบไม่ได้',
  ownerOnly: 'เฉพาะเจ้าของคลินิก (สิทธิ์ settings:danger) ที่เผยแพร่ได้ บัญชีนี้แก้ร่างได้อย่างเดียว',
  open: 'เผยแพร่ร่างนี้',
  label: 'เลขฉบับ',
  labelHint: 'ภาษาอังกฤษ ตัวเลข จุด ขีดล่าง หรือขีดกลาง เช่น 2026-11 ห้ามใช้ 2026-10 ซึ่งเป็นฉบับเดิมที่เขียนในโค้ดหน้าเว็บ',
  effectiveDate: 'วันที่มีผล',
  effectiveHint: 'วันนี้หรือหลังจากนี้ ฉบับนี้เป็นฉบับปัจจุบันตั้งแต่กดเผยแพร่ วันที่นี้คือวันที่พิมพ์บนหน้า',
  legalReview: 'ทนายความหรือที่ปรึกษากฎหมายตรวจถ้อยคำฉบับนี้แล้ว',
  legalReviewHint: 'ถ้าไม่ติ๊ก หน้า /privacy จะแสดงกล่องบอกว่าประกาศยังไม่ได้ผ่านการตรวจของนักกฎหมาย',
  confirm: 'ฉันตรวจเนื้อหาแล้ว และเข้าใจว่าเผยแพร่แล้วแก้ไม่ได้',
  submit: 'ยืนยันเผยแพร่',
  publishing: 'กำลังเผยแพร่…',
  cancel: 'ยกเลิก',
  unsaved: 'บันทึกร่างก่อนเผยแพร่',
  done: 'เผยแพร่แล้ว',
};

export const publishedViewText = {
  heading: 'รายละเอียดฉบับนี้',
  label: 'เลขฉบับ',
  effective: 'วันที่มีผล',
  publishedAt: 'เผยแพร่เมื่อ',
  legal: 'นักกฎหมายตรวจแล้ว',
  legalYes: 'ใช่',
  legalNo: 'ยังไม่ได้ตรวจ หน้าสาธารณะแสดงกล่องเตือน',
  fixed: 'ฉบับที่เผยแพร่แล้วแก้ไม่ได้ ถ้าจะเปลี่ยนให้คัดลอกเป็นร่างใหม่',
  viewPublic: 'เปิดหน้าสาธารณะของฉบับนี้',
};
