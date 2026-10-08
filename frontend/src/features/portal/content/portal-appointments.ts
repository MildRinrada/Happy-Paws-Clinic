import type { AppointmentStatus } from '@/features/appointments/types/appointment.types';

/** Everything the "นัดหมาย" page says. Edit the wording here. */
export const portalAppointments = {
  title: 'นัดหมาย',
  intro: 'นัดที่กำลังจะถึง และนัดที่ผ่านมาของน้องทุกตัว',

  upcomingTitle: 'นัดที่กำลังจะถึง',
  upcomingEmpty: 'ยังไม่มีนัดที่กำลังจะถึง',
  historyTitle: 'นัดที่ผ่านมา',
  historyEmpty: 'ยังไม่มีประวัตินัด',

  serviceFallback: 'นัดหมาย',
  vetFallback: 'สัตวแพทย์ที่ว่างในวันนั้น',
  petPrefix: 'น้อง',

  reschedule: 'เลื่อนนัด',
  lateCancelled: 'ยกเลิกกระชั้นชิด นับตามนโยบายของคลินิก',

  cancel: {
    open: 'ยกเลิกนัด',
    title: 'ยกเลิกนัดนี้',
    policy:
      'ยกเลิกเองได้ทุกเมื่อ แต่ถ้ายกเลิกกระชั้นกว่าเวลาที่คลินิกกำหนด จะนับเป็นการยกเลิกกระชั้นชิด ซึ่งนับรวมกับการไม่มาตามนัด ถ้าเกินจำนวนครั้งที่คลินิกยอมให้ คลินิกอาจเก็บค่าปรับตอนมาครั้งถัดไป',
    rescheduleHint: 'ถ้าแค่อยากเปลี่ยนวันหรือเวลา',
    rescheduleInstead: 'เลื่อนนัดแทน',
    reason: 'เหตุผล (ไม่บังคับ)',
    reasonPlaceholder: 'เช่น ติดธุระ น้องหายดีแล้ว',
    submit: 'ยืนยันยกเลิกนัด',
    submitting: 'กำลังยกเลิก',
    keep: 'ไม่ยกเลิก',
    unreachable: 'เชื่อมต่อระบบไม่ได้ กรุณาลองใหม่อีกครั้ง',
  },

  status: {
    draft: 'ร่าง',
    booked: 'รอยืนยัน',
    confirmed: 'ยืนยันแล้ว',
    checked_in: 'เช็กอินแล้ว',
    in_consultation: 'กำลังตรวจ',
    completed: 'เสร็จแล้ว',
    cancelled: 'ยกเลิก',
    no_show: 'ไม่ได้มาตามนัด',
  } satisfies Record<AppointmentStatus, string>,
};
