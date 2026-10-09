/** The manual points form on `/crm/loyalty` (docs/contract-extensions.md § 25). */
export const pointsAdjustText = {
  heading: 'ปรับแต้มด้วยมือ',
  hint: 'ให้แต้มชดเชยหรือแก้แต้มที่ผิด ต้องระบุเหตุผลทุกครั้ง การปรับเปลี่ยนแต้มคงเหลือ แต่ไม่เปลี่ยนระดับสมาชิก',
  give: 'ให้แต้ม',
  take: 'หักแต้ม',
  direction: 'ทำอะไร',
  points: 'จำนวนแต้ม',
  reason: 'เหตุผล',
  reasonPlaceholder: 'เช่น ชดเชยนัดที่คลินิกเลื่อน',
  submit: 'บันทึกการปรับแต้ม',
  submitting: 'กำลังบันทึก…',
  done: (balance: number) => `บันทึกแล้ว แต้มคงเหลือ ${balance.toLocaleString('th-TH')} แต้ม`,
  wholeNumber: 'กรอกจำนวนเต็มตั้งแต่ 1 ถึง 100,000',
  needReason: 'ต้องระบุเหตุผล',
};

/** The expiry lines beside the balance (§ 25). */
export const pointsExpiryText = {
  next: (points: number, day: string) => `${points.toLocaleString('th-TH')} แต้มจะหมดอายุ ${day}`,
  expired: (points: number) => `หมดอายุไปแล้ว ${points.toLocaleString('th-TH')} แต้ม`,
  rule: (months: number) => `แต้มที่ได้ตั้งแต่ตอนนี้หมดอายุใน ${months} เดือน`,
};
