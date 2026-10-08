import type { ExportStatus, ReportKind } from '../types/report-export.types';

/** The export control on the dashboard. */
export const reportExport = {
  button: 'ส่งออกรายงาน',
  starting: 'กำลังส่งคำขอ…',
  report: 'รายงาน',
  range: (from: string, to: string) => `ช่วง ${from} ถึง ${to} ของสาขาที่เลือก เป็นไฟล์ CSV เปิดด้วย Excel ได้`,
  download: (fileName: string) => `ดาวน์โหลด ${fileName}`,
  rows: (count: number) => `${count} แถว`,
  expires: 'ลิงก์ใช้ได้ 1 ชั่วโมง ถ้าหมดอายุให้กดดูสถานะอีกครั้ง',
  refresh: 'ดูสถานะอีกครั้ง',
  stillWaiting: 'ยังสร้างไฟล์ไม่เสร็จ ระบบทำงานเบื้องหลัง กดดูสถานะอีกครั้งภายหลังได้',
  failed: 'สร้างไฟล์ไม่สำเร็จ ลองส่งออกใหม่อีกครั้ง',
  unreachable: 'ติดต่อระบบไม่ได้ ลองอีกครั้ง',

  reports: {
    revenue: 'รายรับรายวัน แยกสาขาและช่องทางชำระ',
    services: 'รายได้ต่อบริการ',
    retention: 'ลูกค้าใหม่และกลับมา (รายเดือน)',
    staff_performance: 'ผลงานรายบุคคล',
    appointments: 'สถิตินัดหมาย',
    queue: 'สถิติคิว',
    workload: 'ภาระงานของสัตวแพทย์',
  } satisfies Record<ReportKind, string>,

  statuses: {
    queued: 'รอคิวสร้างไฟล์',
    running: 'กำลังสร้างไฟล์',
    succeeded: 'ไฟล์พร้อมแล้ว',
    failed: 'สร้างไฟล์ไม่สำเร็จ',
  } satisfies Record<ExportStatus, string>,
};
