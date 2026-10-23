import { nowIso } from '../../core/db';
import type { ReportFormat, ReportKind } from '@/features/reporting/types/report-export.types';
import {
  appointmentStats,
  queueStats,
  retentionAnalytics,
  revenueAnalytics,
  serviceAnalytics,
  staffPerformance,
  workloadStats,
  type Span,
} from './analytics';
import { dataUrl, renderPdf, toCsv, toXlsx, type Cell } from './documents';
import { branchName, reportExports, type ExportRow } from './store';

/**
 * "ส่งออกรายงาน": a job that is queued, runs for a moment and finishes with a file. The file is the
 * same report the screen shows for the range, as a table, built when the finished job is read.
 */

export const READ_SCOPE: Record<ReportKind, string> = {
  appointments: 'stats:read',
  queue: 'stats:read',
  workload: 'stats:read',
  revenue: 'analytics:read',
  services: 'analytics:read',
  retention: 'analytics:read',
  staff_performance: 'analytics:hr',
};

const TITLES: Record<ReportKind, string> = {
  revenue: 'รายงานรายได้',
  services: 'รายได้ตามบริการ',
  retention: 'การกลับมาใช้บริการของลูกค้า',
  staff_performance: 'ผลงานรายบุคคล',
  appointments: 'สถิตินัดหมาย',
  queue: 'สถิติคิว',
  workload: 'ภาระงานแพทย์',
};

const baht = (satang: number) => Math.round(satang) / 100;
const minutes = (value?: number) => (value === undefined ? '' : value);

/** The report as a table: a header row, then one row per period, service or person. */
export function reportTable(report: ReportKind, span: Span): Cell[][] {
  switch (report) {
    case 'revenue': {
      const data = revenueAnalytics(span);
      return [
        ['งวด', 'รับชำระ (บาท)', 'คืนเงิน (บาท)', 'สุทธิ (บาท)'],
        ...data.series.map((row) => [row.period, baht(row.collected_satang), baht(row.refunded_satang), baht(row.net_satang)]),
        ['รวม', baht(data.totals.collected_satang), baht(data.totals.refunded_satang), baht(data.totals.net_satang)],
      ];
    }
    case 'services': {
      const data = serviceAnalytics(span);
      return [
        ['บริการ', 'จำนวน', 'บิล', 'ยอดตามราคา (บาท)', 'รายได้ (บาท)', 'ค่ายา (บาท)', 'หลังหักยา (บาท)'],
        ...data.services.map((row) => [row.name, row.quantity, row.invoices, baht(row.billed_satang), baht(row.revenue_satang), baht(row.medicine_satang), baht(row.revenue_excluding_medicine_satang)]),
      ];
    }
    case 'retention': {
      const data = retentionAnalytics(span);
      return [['เดือน', 'ลูกค้าที่มาใช้บริการ', 'ลูกค้าใหม่', 'ลูกค้าเก่า'], ...data.months.map((row) => [row.month.slice(0, 7), row.active, row.new, row.returning])];
    }
    case 'staff_performance': {
      const data = staffPerformance(span);
      return [
        ['ชื่อ', 'เคส', 'เวลาเฉลี่ยต่อเคส (นาที)', 'หัตถการ', 'รายได้จากเคส (บาท)', 'รับชำระที่เคาน์เตอร์ (บาท)'],
        ...data.staff.map((row) => [row.display_name, row.cases, minutes(row.average_case_minutes), row.treatments, baht(row.attributed_revenue_satang), baht(row.collected_satang)]),
      ];
    }
    case 'appointments': {
      const data = appointmentStats(span);
      return [
        ['งวด', 'นัดทั้งหมด', 'มาตามนัด', 'ยกเลิก', 'ไม่มาตามนัด'],
        ...data.series.map((row) => [row.period, row.total, row.completed, row.cancelled, row.no_show]),
        ['รวม', data.totals.total, data.totals.completed, data.totals.cancelled, data.totals.no_show],
      ];
    }
    case 'queue': {
      const data = queueStats(span);
      return [['วันที่', 'บัตรคิว', 'ให้บริการแล้ว', 'รอเฉลี่ย (นาที)'], ...data.series.map((row) => [row.date, row.tickets, row.served, minutes(row.average_wait_minutes)])];
    }
    case 'workload': {
      const data = workloadStats(span);
      return [
        ['ชื่อ', 'เคส', 'เวลาเฉลี่ยต่อเคส (นาที)', 'นัดหมาย', 'เคสค้าง', 'คิวรอ'],
        ...data.staff.map((row) => [row.display_name ?? row.staff_id, row.cases, minutes(row.average_case_minutes), row.appointments, row.open_cases, row.waiting_queue]),
      ];
    }
  }
}

const TYPES: Record<ReportFormat, string> = {
  csv: 'text/csv',
  xlsx: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  pdf: 'application/pdf',
};

/** Built once per job and kept for the page's life: a poll every two seconds should not redraw a PDF. */
const built = new Map<string, { url: string; size: number; rows: number }>();

function build(job: ExportRow, span: Span) {
  const cached = built.get(job.id);
  if (cached) return cached;
  const table = reportTable(job.report, span);
  const title = TITLES[job.report];
  const subtitle = `${branchName(job.branch_id) ?? 'ทุกสาขา'} · ${job.from} ถึง ${job.to}`;
  let url: string;
  let size: number;
  if (job.format === 'csv') {
    const text = toCsv([[title], [subtitle], [], ...table]);
    size = new TextEncoder().encode(text).length;
    url = dataUrl(text, TYPES.csv);
  } else if (job.format === 'xlsx') {
    const bytes = toXlsx(title, table);
    size = bytes.length;
    url = dataUrl(bytes, TYPES.xlsx);
  } else {
    const [header = [], ...rows] = table;
    const bytes = renderPdf(title, subtitle, [{ cells: header, bold: true }, ...rows.map((cells) => ({ cells: cells.map((cell) => (typeof cell === 'number' ? cell.toLocaleString('th-TH') : cell)) }))]);
    size = bytes.length;
    url = dataUrl(bytes, TYPES.pdf);
  }
  const result = { url, size, rows: Math.max(0, table.length - 1) };
  built.set(job.id, result);
  return result;
}

/** The job as it stands now: queued for half a second, running for a few more, then done. */
export function advance(job: ExportRow, span: Span): ExportRow {
  const age = Date.now() - Date.parse(job.created_at);
  if (job.status === 'queued' && age > 500) {
    job = reportExports().update(job.id, { status: 'running', started_at: nowIso() })!;
  }
  if (job.status === 'running' && age > 2_500) {
    let file: { url: string; size: number; rows: number };
    try {
      file = build(job, span);
    } catch (error) {
      console.error('[mock] report export failed', error);
      return reportExports().update(job.id, { status: 'failed', completed_at: nowIso(), error_code: 'render_failed' })!;
    }
    job = reportExports().update(job.id, {
      status: 'succeeded',
      completed_at: nowIso(),
      file_name: `${job.report}_${job.from}_${job.to}.${job.format}`,
      size_bytes: file.size,
      row_count: file.rows,
    })!;
  }
  if (job.status === 'succeeded') {
    // Signed afresh on each read, as the product's storage does. The bytes live only in this page.
    const file = build(job, span);
    return { ...job, download_url: file.url, download_expires_at: new Date(Date.now() + 3_600_000).toISOString() };
  }
  return job;
}
