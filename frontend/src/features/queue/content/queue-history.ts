import type { HistoryFilter } from '../types/queue.types';

/** The history view: a day's tickets, finished ones by default, any day by the date picker. */
export const queueHistory = {
  metaTitle: 'ประวัติคิว · Happy Paws Clinic',
  title: 'ประวัติคิว',
  subtitle: (branch: string, date: string, count: number) => `${branch} · ${date} · ${count} ใบ`,
  back: 'กลับไปกระดานคิว',
  noBranch: 'ยังไม่มีสาขาที่บัญชีนี้เข้าถึงได้',
  noAccess: 'บัญชีนี้ไม่มีสิทธิ์ดูคิว (queue:read)',

  filters: {
    date: 'วันที่',
    status: 'สถานะ',
    submit: 'ดู',
  },

  statusOptions: {
    finished: 'จบแล้วทั้งหมด',
    served: 'ตรวจเสร็จ',
    left: 'ออกจากคิว',
    all: 'ทุกสถานะ',
  } satisfies Record<HistoryFilter, string>,

  empty: 'ไม่มีบัตรคิวที่ตรงกับตัวกรองในวันนี้',

  columns: {
    number: 'เลขคิว',
    pet: 'สัตว์เลี้ยง',
    service: 'บริการ',
    status: 'สถานะ',
    vet: 'สัตวแพทย์',
    room: 'ห้อง',
    issued: 'ออกบัตร',
    called: 'เรียก',
  },

  none: 'ไม่ระบุ',
  skipped: (count: number) => `ถูกข้าม ${count} ครั้ง`,
  limitations: 'เหตุผลที่ออกจากคิวและเวลาที่ตรวจเสร็จยังไม่มีในข้อมูลที่ API ส่งมา',
} as const;
