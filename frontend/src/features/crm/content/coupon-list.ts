import type { BadgeTone } from '@/components/ui/badge';
import type { CouponStatus } from '../types/crm.types';

/** `/crm/coupons`, the list under the form: the coupons issued and switching one off. */
export const couponListText = {
  heading: 'คูปองที่ออกแล้ว',
  hint: 'ใหม่สุดก่อน จำนวนที่ใช้นับเฉพาะบิลที่ไม่ถูกยกเลิก',
  empty: 'ยังไม่มีคูปองตามตัวกรองนี้',
  noRead: 'บัญชีนี้ดูรายการคูปองไม่ได้ (ต้องมีสิทธิ์ crm:read)',

  filters: {
    label: 'กรองคูปอง',
    search: 'ค้นโค้ดหรือชื่อ',
    status: 'สถานะ',
    any: 'ทั้งหมด',
    apply: 'กรอง',
  },

  columns: {
    code: 'โค้ด',
    discount: 'ส่วนลด',
    period: 'ช่วงใช้ได้',
    used: 'ใช้ไป',
    status: 'สถานะ',
    action: '',
  },

  percent: (value: number) => `${value.toLocaleString('th-TH')}%`,
  from: (day: string) => `ตั้งแต่ ${day}`,
  range: (from: string, until: string) => `${from} ถึง ${until}`,
  used: (count: number, max?: number) =>
    max === undefined ? `${count.toLocaleString('th-TH')} ครั้ง` : `${count.toLocaleString('th-TH')} จาก ${max.toLocaleString('th-TH')}`,
  allBranches: 'ทุกสาขา',
  deactivatedNote: (reason?: string) => (reason ? `ปิดแล้ว: ${reason}` : 'ปิดแล้ว'),
  next: 'หน้าถัดไป',
  first: 'กลับหน้าแรก',

  deactivate: {
    button: 'ปิดคูปอง',
    confirm: 'บิลที่ใช้ไปแล้วไม่เปลี่ยน แต่ใช้โค้ดนี้กับบิลใหม่ไม่ได้อีก และเปิดกลับไม่ได้',
    confirmButton: 'ปิดเลย',
    pending: 'กำลังปิด…',
    back: 'ไม่ปิด',
  },
};

/** A coupon's state, with the badge colour it is drawn in. */
export const couponStatusLabels: Record<CouponStatus, { text: string; tone: BadgeTone }> = {
  active: { text: 'ใช้ได้', tone: 'green' },
  scheduled: { text: 'ยังไม่เริ่ม', tone: 'blue' },
  used_up: { text: 'ใช้ครบแล้ว', tone: 'neutral' },
  expired: { text: 'หมดอายุ', tone: 'neutral' },
  deactivated: { text: 'ปิดแล้ว', tone: 'red' },
};
