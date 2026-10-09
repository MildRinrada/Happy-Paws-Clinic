/**
 * Everything the portal chat page says (`/portal/messages`). Edit the wording here.
 *
 * The conversation is the same one the clinic answers from its "ข้อความลูกค้า" page, so a message
 * sent to the clinic's LINE account from a linked LINE shows here too.
 */
export const portalChat = {
  title: 'แชทกับคลินิก',
  intro: 'พิมพ์ถึงคลินิกได้ที่นี่ เจ้าหน้าที่ตอบในเวลาทำการ คำตอบขึ้นในหน้านี้และในแจ้งเตือน พร้อมส่งทาง LINE, SMS หรืออีเมลตามที่คุณตั้งไว้',
  urgent: 'ถ้าน้องป่วยฉุกเฉิน กรุณาโทรหาคลินิกทันที อย่ารอคำตอบในแชท',

  listLabel: 'ข้อความกับคลินิก',
  empty: 'ยังไม่มีข้อความ เริ่มพิมพ์ถึงคลินิกได้เลย',
  you: 'คุณ',
  clinic: 'คลินิก',
  /** A customer's own message that came by another channel, so they know why it is here. */
  sentVia: (channel: string) => `ส่งทาง ${channel}`,
  channels: { line: 'LINE', sms: 'SMS', email: 'อีเมล' } as Record<string, string>,

  /** What the customer sent when it was not words: only words are kept, the rest stays in LINE. */
  kinds: {
    image: 'รูปภาพ (ดูได้ในแอป LINE)',
    video: 'วิดีโอ (ดูได้ในแอป LINE)',
    audio: 'ข้อความเสียง (ฟังได้ในแอป LINE)',
    file: 'ไฟล์',
    location: 'ตำแหน่ง',
    sticker: 'สติกเกอร์',
    other: 'ข้อความชนิดที่แสดงในหน้านี้ไม่ได้',
  } as Record<string, string>,

  awaiting: 'ส่งถึงคลินิกแล้ว เจ้าหน้าที่จะตอบกลับในเวลาทำการ',

  older: 'ดูข้อความก่อนหน้า',
  newest: 'กลับไปข้อความล่าสุด',

  compose: {
    label: 'ข้อความถึงคลินิก',
    placeholder: 'พิมพ์ข้อความ',
    hint: 'ไม่เกิน 1,000 ตัวอักษร',
    send: 'ส่ง',
    sending: 'กำลังส่ง',
    empty: 'พิมพ์ข้อความก่อนส่ง',
    unreachable: 'ส่งไม่สำเร็จ เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },
};
