import type { DueBackReason } from '../types/due-back.types';

/** "ใครควรกลับมาแล้วแต่ยังไม่มา" (docs/contract-extensions.md § 33). */
export const dueBackPage = {
  title: 'ควรกลับมาแล้ว',
  subtitle: (branch: string) => `รายชื่อลูกค้าของ${branch} ที่ควรโทรหา คิดจากข้อมูลของลูกค้าแต่ละคน ไม่ใช่เกณฑ์เดียวกันทุกคน`,
  reasonsLabel: 'เหตุผล',
  send: 'ส่งข้อความหาลูกค้า',
  empty: 'ตอนนี้ไม่มีใครในรายการนี้',
  next: 'หน้าถัดไป',
  first: 'กลับหน้าแรก',

  overdueFor: (days: number) => `เลยมา ${days} วัน`,
  dueOn: (day: string) => `ครบกำหนด ${day}`,
  dueBackOn: (day: string) => `ควรกลับมาภายใน ${day}`,
  lastVisit: (day: string) => `มาครั้งล่าสุด ${day}`,
  neverVisited: 'ยังไม่มีบันทึกการมาที่สาขานี้',
  rhythm: (count: number, median: number, allowed: number) =>
    `มาแล้ว ${count} ครั้ง ปกติห่างกันราว ${median} วัน ถือว่าหายไปเมื่อเกิน ${allowed} วัน`,
  vaccine: (name: string, day: string) => `${name} ครบกำหนด ${day}`,
  openCustomer: 'ดูลูกค้า',
};

export const dueBackReasons: Record<DueBackReason, { label: string; hint: string }> = {
  vaccine_overdue: {
    label: 'วัคซีนเลยกำหนด',
    hint: 'สัตว์ที่ยังมีชีวิต วัคซีนเข็มล่าสุดของชนิดใดชนิดหนึ่งเลยวันครบกำหนดแล้ว และยังไม่มีนัดล่วงหน้า เรียงจากที่เพิ่งเลยกำหนดก่อน',
  },
  lapsed: {
    label: 'หายไปนานกว่าปกติ',
    hint: 'ลูกค้าที่มาอย่างน้อย 3 ครั้ง และหายไปนานกว่ารอบปกติของตัวเอง (มากกว่า 1.5 เท่าของระยะห่างกลาง หรือระยะห่างกลางบวก 30 วัน แล้วแต่ค่าไหนมากกว่า) ยังมีสัตว์ที่มีชีวิต และยังไม่มีนัดล่วงหน้า',
  },
};
