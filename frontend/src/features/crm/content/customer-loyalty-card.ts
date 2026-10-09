/** The small points card on a customer's page. */
export const customerLoyaltyCard = {
  title: 'แต้มและแพ็กเกจ',
  points: (count: number) => `${count.toLocaleString('th-TH')} แต้ม`,
  noPackages: 'ไม่มีแพ็กเกจที่ใช้ได้',
  failed: 'โหลดข้อมูลแต้มไม่ได้ในตอนนี้',
  open: 'ดูประวัติแต้มและแพ็กเกจ',
};
