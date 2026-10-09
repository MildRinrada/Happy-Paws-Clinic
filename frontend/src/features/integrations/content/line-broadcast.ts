/** The broadcast page under "แจ้งเตือนลูกค้า". */
export const lineBroadcastPage = {
  metaTitle: 'ประกาศทาง LINE · Happy Paws Clinic',
  title: 'ประกาศทาง LINE',
  subtitle:
    'ส่งข้อความถึงลูกค้าที่เลือกทาง LINE ถือเป็นข้อความการตลาด จึงถึงเฉพาะลูกค้าที่ยินยอมรับข่าวสาร และมี LINE ในข้อมูลลูกค้า ไม่ส่งทาง SMS แทน',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ส่งประกาศ',
  noCustomerSearch: 'บัญชีนี้ค้นหาลูกค้าไม่ได้ (ต้องมีสิทธิ์ customer:read) จึงเลือกผู้รับไม่ได้',
  segmentNote:
    'เลือกลูกค้าทีละคน ไม่เกิน 200 คนต่อครั้ง หรือสร้างกลุ่มลูกค้าตามเงื่อนไขที่หน้าแพ็กเกจและโปรโมชัน แท็บกลุ่มลูกค้า แล้วกดส่งประกาศจากที่นั่น',
  segmentTarget: (name: string, reachable?: number) =>
    reachable === undefined ? `ส่งถึงกลุ่ม ${name}` : `ส่งถึงกลุ่ม ${name} (ประมาณ ${reachable.toLocaleString('th-TH')} คน)`,
  segmentHint: 'ระบบคัดสมาชิกกลุ่มใหม่ตอนกดส่ง ส่งถึงเฉพาะคนที่ยินยอมรับข่าวสารและมี LINE ถ้าเกิน 200 คนระบบจะไม่ส่ง',
  segmentClear: 'เลือกลูกค้าเองแทน',
  recipients: 'ผู้รับ',
  maxRecipients: 200,
  tooMany: 'ประกาศได้ไม่เกิน 200 คนต่อครั้ง',
  message: 'ข้อความประกาศ',
  messageHint: 'ไม่เกิน 1,000 ตัวอักษร',
  submit: 'ส่งประกาศ',
  sending: 'กำลังส่ง…',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',
};
