/** The bill as shown on its page, and the receipt card beside it. */
export const invoiceDocument = {
  approvedBy: (name: string) => `อนุมัติโดย ${name}`,
  percent: (value: number) => `(${value}%)`,
  refundTotal: 'คืนเงิน',
  receiptRefunded: (amount: string) => `บิลนี้คืนเงินแล้ว ${amount} ใบเสร็จแสดงยอดที่รับชำระ ไม่หักเงินที่คืน และไม่ออกใบใหม่`,
  receiptRefundedBefore: (amount: string) => `บิลนี้คืนเงินแล้ว ${amount} ใบเสร็จที่ออกจะแสดงยอดที่รับชำระทั้งหมด ไม่หักเงินที่คืน`,
};
