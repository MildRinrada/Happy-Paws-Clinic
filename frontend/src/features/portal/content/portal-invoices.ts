import type { InvoiceStatus, PaymentMethod } from '../types/invoice.types';

/** Everything the "บิลและใบเสร็จ" page says. Edit the wording here. */
export const portalInvoices = {
  title: 'บิลและใบเสร็จ',
  intro: 'บิลจากการมาคลินิกทุกครั้ง ยอดที่ชำระแล้ว ยอดที่ยังค้าง และเลขที่ใบเสร็จ',

  outstanding: {
    title: 'ยอดค้างชำระ',
    count: (count: number, more: boolean) => `จาก ${count}${more ? '+' : ''} บิล`,
    howToPay: 'ชำระได้ที่เคาน์เตอร์คลินิก ยังชำระผ่านเว็บไม่ได้',
    none: 'ไม่มียอดค้างชำระ',
  },

  tabs: {
    all: 'ทั้งหมด',
    unpaid: (count: number, more: boolean) => `ค้างชำระ (${count}${more ? '+' : ''})`,
  },

  empty: 'ยังไม่มีบิล',
  emptyUnpaid: 'ไม่มีบิลที่ค้างชำระ',
  newest: 'กลับไปบิลล่าสุด',
  older: 'บิลเก่ากว่านี้',

  number: (number: string) => `บิลเลขที่ ${number}`,
  forPet: (name: string) => `น้อง${name}`,
  total: 'ยอดรวม',
  balanceDue: (amount: string) => `ค้าง ${amount}`,
  showDetail: 'ดูรายการ',

  status: {
    open: 'รอชำระ',
    partially_paid: 'ชำระบางส่วน',
    paid: 'ชำระแล้ว',
    refunded: 'คืนเงินแล้ว',
    void: 'ยกเลิกบิล',
  } satisfies Record<InvoiceStatus, string>,

  lines: {
    title: 'รายการ',
    quantity: (quantity: number, unit: string) => `${quantity} x ${unit}`,
  },

  totals: {
    subtotal: 'รวมรายการ',
    discount: 'ส่วนลด',
    total: 'ยอดรวม',
    paid: 'ชำระแล้ว',
    refunded: 'คืนเงิน',
    balance: 'ค้างชำระ',
  },

  discountReason: (reason: string) => `ส่วนลด: ${reason}`,

  payments: {
    title: 'การชำระเงิน',
    refund: (reason: string) => `คืนเงิน: ${reason}`,
  },

  method: {
    cash: 'เงินสด',
    card: 'บัตร',
    promptpay: 'พร้อมเพย์',
  } satisfies Record<PaymentMethod, string>,

  receipt: {
    title: 'ใบเสร็จ',
    number: (number: string) => `เลขที่ ${number}`,
    taxInvoice: 'ใบกำกับภาษี',
    issued: (date: string) => `ออกเมื่อ ${date}`,
    none: 'ยังไม่ได้ออกใบเสร็จ ขอได้ที่เคาน์เตอร์',
  },

  voided: (reason: string) => `บิลนี้ถูกยกเลิก: ${reason}`,
  note: (note: string) => `หมายเหตุ: ${note}`,
};
