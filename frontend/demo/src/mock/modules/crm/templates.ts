/**
 * The message catalogue, converted from the backend's `NotificationTemplateCatalogue.cs`: every
 * message the clinic sends, worded once per channel. `{name}` is a value and `[ ... ]` words that
 * only stay when every value inside them is present.
 */

export interface TemplateDef {
  key: string;
  name: string;
  purpose: 'service_messages' | 'marketing';
  staff_sendable: boolean;
  variables: Array<{ name: string; required: boolean; max_length: number }>;
  title: string;
  line: string;
  sms: string;
}

const v = (name: string, required = true, max_length = 100) => ({ name, required, max_length });

export const TEMPLATES: TemplateDef[] = [
  {
    key: 'appointment.reminder',
    name: 'เตือนนัดหมาย',
    purpose: 'service_messages',
    staff_sendable: true,
    variables: [v('pet_name'), v('date', true, 40), v('time', true, 20), v('vet_name', false)],
    title: 'เตือนนัดหมาย',
    line: 'เตือนนัดหมาย {pet_name} วันที่ {date} เวลา {time} น.[ กับ{vet_name}] หากมาไม่ได้กรุณาแจ้งคลินิกล่วงหน้า',
    sms: '{clinic}: นัด{pet_name} {date} {time}น.[ {vet_name}]',
  },
  {
    key: 'vaccine.due',
    name: 'ครบกำหนดวัคซีน',
    purpose: 'service_messages',
    staff_sendable: true,
    variables: [v('pet_name'), v('date', true, 40), v('vaccine_name', false)],
    title: 'ครบกำหนดฉีดวัคซีน',
    line: '{pet_name} ครบกำหนดฉีดวัคซีน[ {vaccine_name}] วันที่ {date} นัดเวลาฉีดกับคลินิกได้เลย',
    sms: '{clinic}: {pet_name} ครบกำหนดวัคซีน {date}',
  },
  {
    key: 'treatment_plan.session',
    name: 'เตือนรอบแผนการรักษา',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('pet_name'), v('plan_title'), v('session_number', true, 3), v('date', true, 40), v('time', false, 20)],
    title: 'ถึงรอบ{plan_title}',
    line: '{pet_name} มีรอบ{plan_title} ครั้งที่ {session_number} วันที่ {date}[ เวลา {time} น.] หากยังไม่ได้นัดหรือมาไม่ได้ กรุณาติดต่อคลินิก',
    sms: '{clinic}: {pet_name} รอบรักษาครั้งที่ {session_number} {date}',
  },
  {
    key: 'clinic.message',
    name: 'ข้อความถึงเจ้าของ',
    purpose: 'service_messages',
    staff_sendable: true,
    variables: [v('message', true, 500)],
    title: 'ข้อความจากคลินิก',
    line: '{message}',
    sms: '{clinic}: {message}',
  },
  {
    key: 'thread.reply',
    name: 'ตอบข้อความลูกค้า',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('message', true, 1000)],
    title: 'ข้อความตอบกลับจากคลินิก',
    line: '{message}',
    sms: '{clinic}: {message}',
  },
  {
    key: 'queue.called',
    name: 'เรียกคิว',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('number'), v('room', false)],
    title: 'ถึงคิว {number} แล้ว',
    line: 'ถึงคิว {number} แล้ว เชิญที่ห้องตรวจ[ {room}]',
    sms: '{clinic}: ถึงคิว {number} แล้ว เชิญที่ห้องตรวจ[ {room}]',
  },
  {
    key: 'receipt.issued',
    name: 'ใบเสร็จ',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('receipt_number'), v('amount'), v('invoice_id', false)],
    title: 'ใบเสร็จ {receipt_number}',
    line: 'ได้รับชำระ {amount} บาท ใบเสร็จเลขที่ {receipt_number} ขอบคุณที่ใช้บริการ',
    sms: '{clinic}: ชำระ {amount} บาท ใบเสร็จ {receipt_number}',
  },
  {
    key: 'consultation.summary',
    name: 'สรุปการตรวจ',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('record_id', false)],
    title: 'สรุปการตรวจ',
    line: 'คุณหมอบันทึกผลการตรวจเรียบร้อยแล้ว ดูรายละเอียดได้ที่[ {portal_url}]',
    sms: '{clinic}: บันทึกผลการตรวจแล้ว ดูได้ในระบบออนไลน์',
  },
  {
    key: 'waiting_list.slot_available',
    name: 'มีคิวว่าง',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('pet_name'), v('service_name'), v('date'), v('time')],
    title: 'มีคิวว่างแล้ว',
    line: '{service_name} สำหรับ {pet_name} ว่างวันที่ {date} เวลา {time} น. จองได้เลย ใครจองก่อนได้ก่อน',
    sms: '{clinic}: คิวว่าง {date} {time}น. จองได้เลย',
  },
  {
    key: 'pet.deceased_appointments_cancelled',
    name: 'ยกเลิกนัดของสัตว์ที่เสียชีวิต',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [],
    title: 'ยกเลิกนัดหมายแล้ว',
    line: 'คลินิกขอแสดงความเสียใจ นัดหมายที่เหลือทั้งหมดถูกยกเลิกแล้ว',
    sms: '{clinic}: ขอแสดงความเสียใจ นัดที่เหลือถูกยกเลิกแล้ว',
  },
  {
    key: 'customer.portal_invitation',
    name: 'ชวนเปิดบัญชีออนไลน์',
    purpose: 'service_messages',
    staff_sendable: false,
    variables: [v('display_name', false)],
    title: 'ยินดีต้อนรับสู่ระบบออนไลน์ของคลินิก',
    line: 'ดูนัดหมาย ประวัติ และใบเสร็จของสัตว์เลี้ยงได้ที่[ {portal_url}] สมัครด้วยเบอร์โทรที่ให้ไว้กับคลินิก',
    sms: '{clinic}: เปิดบัญชีออนไลน์ด้วยเบอร์นี้ ดูนัดและประวัติสัตว์เลี้ยง',
  },
  {
    key: 'customer.lapsed',
    name: 'คิดถึงน้อง (ลูกค้าที่หายไป)',
    purpose: 'marketing',
    staff_sendable: false,
    variables: [v('pet_name', true, 200), v('date', false, 40)],
    title: 'คลินิกคิดถึง {pet_name}',
    line: 'ไม่ได้เจอ {pet_name} มาสักพักแล้ว ถ้าถึงเวลาตรวจสุขภาพหรือมีเรื่องอยากปรึกษา นัดกับคลินิกได้เลย',
    sms: '{clinic}: คิดถึง{pet_name} นัดตรวจสุขภาพได้เลย',
  },
  {
    key: 'review.invitation',
    name: 'ชวนรีวิวหลังมาใช้บริการ',
    purpose: 'marketing',
    staff_sendable: false,
    variables: [v('pet_name', true, 200), v('date', false, 40), v('review_url', false, 300)],
    title: 'ขอบคุณที่พา {pet_name} มาที่คลินิก',
    line: 'ขอบคุณที่พา {pet_name} มาใช้บริการ[ เมื่อ {date}] ช่วยให้คะแนนและเล่าให้คลินิกฟังได้ที่[ {review_url}] รีวิวขึ้นหน้าเว็บหลังคลินิกอ่านแล้วเท่านั้น',
    sms: '{clinic}: ขอบคุณที่มาใช้บริการ รีวิวได้ที่[ {review_url}]',
  },
  {
    key: 'clinic.announcement',
    name: 'ประกาศ (LINE)',
    purpose: 'marketing',
    staff_sendable: false,
    variables: [v('message', true, 1000)],
    title: 'ประกาศจากคลินิก',
    line: '{message}',
    sms: '{clinic}: {message}',
  },
];

export function templateOf(key: string): TemplateDef | undefined {
  return TEMPLATES.find((t) => t.key === key);
}

/** The backend's `TemplateText.Fill`: values in, optional segments kept only when complete. */
export function fill(pattern: string, values: Record<string, string | undefined>): string {
  const substitute = (text: string) => {
    let complete = true;
    const result = text.replace(/\{([a-z_]+)\}/g, (_, name: string) => {
      const value = values[name];
      if (value && value.trim()) return value.trim();
      complete = false;
      return '';
    });
    return { result, complete };
  };
  let output = '';
  let position = 0;
  while (position < pattern.length) {
    const open = pattern.indexOf('[', position);
    const close = open < 0 ? -1 : pattern.indexOf(']', open + 1);
    if (open < 0 || close < 0) {
      output += substitute(pattern.slice(position)).result;
      break;
    }
    output += substitute(pattern.slice(position, open)).result;
    const segment = substitute(pattern.slice(open + 1, close));
    if (segment.complete) output += segment.result;
    position = close + 1;
  }
  return output.replace(/[ \t]{2,}/g, ' ').trim();
}
