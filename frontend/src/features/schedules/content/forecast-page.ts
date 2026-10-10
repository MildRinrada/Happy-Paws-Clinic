/** "วันไหนจะยุ่ง หมอพอไหม" (docs/contract-extensions.md § 33). */
export const demandForecastPage = {
  metaTitle: 'คาดการณ์ความหนาแน่น · Happy Paws Clinic',
  title: 'คาดการณ์ความหนาแน่น',
  subtitle: (branch: string) => `นัดที่จองแล้ว คนที่น่าจะ walk-in เข้ามา และหมอที่ลงเวรของ${branch} รายชั่วโมง`,
  noAccess: 'บัญชีนี้ดูคาดการณ์ไม่ได้ (stats:read)',
  noBranch: 'บัญชีนี้ยังไม่ได้ประจำสาขาใด',
  link: 'คาดการณ์ความหนาแน่น',
  back: 'กลับไปตารางเวร',

  from: 'ตั้งแต่วันที่',
  span: 'ดูกี่วัน',
  spanOption: (days: number) => `${days} วัน`,
  apply: 'แสดง',

  how: (weeks: number) =>
    `walk-in คาดจากค่าเฉลี่ยของวันเดียวกันในสัปดาห์และชั่วโมงเดียวกัน ย้อนหลัง ${weeks} สัปดาห์ ชั่วโมงที่ติดป้าย "หนาแน่น" คือคนไข้ต่อหมอสูงกว่าปกติของสาขานี้ หรือมีคนไข้แต่ไม่มีหมอลงเวร`,
  line: (value: number) => `เส้น "หนาแน่น" ของสาขานี้: มากกว่า ${value} คนต่อหมอต่อชั่วโมง (สูงกว่า 80% ของชั่วโมงที่ผ่านมา)`,
  noLine: 'ข้อมูลย้อนหลังยังน้อย จึงยังไม่มีเส้น "หนาแน่น" ป้ายจะขึ้นเฉพาะชั่วโมงที่มีคนไข้แต่ไม่มีหมอ',

  dayTotals: (booked: number, walkIns: number) => `นัด ${booked} walk-in ประมาณ ${walkIns}`,
  busyHours: (count: number) => `หนาแน่น ${count} ชั่วโมง`,
  noHours: 'วันนี้ไม่มีเวร ไม่มีนัด และไม่เคยมี walk-in ในวันแบบนี้',

  hour: (hour: number) => `${String(hour).padStart(2, '0')}:00`,
  booked: (count: number) => `นัด ${count}`,
  walkIns: (count: number) => `walk-in ~${count}`,
  vets: (count: number) => `หมอ ${count}`,
  perVet: (load: number) => `${load} คน/หมอ`,
  noVet: 'ไม่มีหมอ',
  busy: 'หนาแน่น',
};

/** The spans offered. The backend accepts up to 14 days. */
export const FORECAST_SPANS = [7, 14];
