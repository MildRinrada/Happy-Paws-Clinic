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
