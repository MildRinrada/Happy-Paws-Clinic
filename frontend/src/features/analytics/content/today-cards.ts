/** The cards for today at the top of the overview page. */
export const todayCards = {
  heading: 'วันนี้',
  updatedAt: (time: string) => `อัปเดตเมื่อ ${time}`,

  appointments: {
    title: 'นัดวันนี้',
    total: 'นัดทั้งหมด',
    upcoming: 'ยังไม่มา',
    arrived: 'มาแล้ว',
    completed: 'เสร็จแล้ว',
    cancelled: 'ยกเลิก',
    noShow: 'ไม่มาตามนัด',
  },

  queue: {
    title: 'คิววันนี้',
    issued: 'ออกบัตรคิว',
    waiting: 'รอเรียก',
    inRoom: 'อยู่ในห้องตรวจ',
    served: 'ตรวจเสร็จ',
    averageWait: 'รอเฉลี่ย',
    longestWait: 'รอนานสุดตอนนี้',
  },

  consultations: {
    title: 'เคสตรวจ',
    open: 'เปิดค้างอยู่',
    closedToday: 'ปิดวันนี้',
  },

  revenue: {
    title: 'รายรับวันนี้',
    net: 'รับสุทธิ',
    collected: 'รับชำระ',
    refunded: 'คืนเงิน',
    payments: 'จำนวนรายการรับชำระ',
  },

  mine: {
    title: 'งานของฉัน',
    appointments: 'นัดของฉันวันนี้',
    queueWaiting: 'คิวที่รอฉัน',
    openConsultations: 'เคสที่ฉันเปิดค้าง',
  },
};
