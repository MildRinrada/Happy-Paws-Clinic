/** The morning brief card on the dashboard. */
export const dailyBriefText = {
  title: 'สรุปเช้านี้',
  pending: (at: string) => `ยังไม่มีสรุปของวันนี้ ระบบสรุปให้หลัง ${at} น.`,
  failed: 'AI เขียนสรุปของวันนี้ไม่ได้ ตัวเลขของวันนี้ด้านบนยังถูกต้อง',
  quota: 'โควตา AI ของเดือนนี้หมดแล้ว วันนี้จึงไม่มีสรุป ตัวเลขของวันนี้อยู่ด้านบน',
  more: 'ดูเพิ่ม',
  less: 'ย่อลง',
} as const;
