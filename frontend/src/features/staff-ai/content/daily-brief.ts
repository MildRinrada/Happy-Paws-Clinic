/** The morning brief card on the dashboard. */
export const dailyBriefText = {
  title: 'สรุปเช้านี้',
  pending: (at: string) => `ยังไม่มีสรุปของวันนี้ ระบบสรุปให้หลัง ${at} น.`,
  failed: 'AI เขียนสรุปของวันนี้ไม่ได้ ตัวเลขด้านล่างยังถูกต้อง',
  quota: 'โควตา AI ของเดือนนี้หมดแล้ว วันนี้จึงมีแค่ตัวเลข',
  appointments: 'นัดวันนี้',
  upcoming: (n: number) => `ยังไม่มา ${n}`,
  walkIn: (n: number) => `walk-in ${n}`,
  queue: 'คิวตอนนี้',
  outstanding: (n: number) => `รอ ${n}`,
  stock: 'สต๊อกที่ต้องดู',
  stockHint: (expired: number, out: number) => `หมดอายุ ${expired} หมด ${out}`,
  revenue: 'รายรับเมื่อวาน (สุทธิ)',
  revenueHint: (payments: number) => `${payments} รายการ`,
} as const;
