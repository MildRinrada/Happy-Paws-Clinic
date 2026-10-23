import { table, type Row } from '../../core/db';
import type { Auth } from '../../core/http';
import type {
  AppointmentStats,
  Granularity,
  LineKind,
  QueueStats,
  RetentionAnalytics,
  RevenueAnalytics,
  ServiceAnalytics,
  StaffPerformance,
  TodayDashboard,
  WorkloadStats,
} from '@/features/analytics/types/analytics.types';
import type { PaymentMethod } from '@/features/billing/types/billing.types';
import { liveInvoices } from './invoices';
import { addDays, branchName, clinicDayOf, clinicHourOf, clinicToday, holdsAny, isVet, serviceOf, staffOf, type InvoiceRow } from './store';

/**
 * Every figure on the dashboard and the reports, worked out from the rows the other pages show:
 * appointments, queue tickets and consultations (read from their modules' tables) and the bills
 * here. Nothing is stored, so a payment taken a moment ago is in the next read.
 */

export interface Span {
  from: string;
  to: string;
  branchIds: string[];
  /** The branch asked for, echoed back as `branch_id`. */
  branchId?: string;
  granularity: Granularity;
}

const inSpan = (dayText: string, span: Span) => dayText >= span.from && dayText <= span.to;
const atBranch = (row: Row, span: { branchIds: string[] }) => span.branchIds.includes(row.branch_id);
const minutes = (from?: string | null, to?: string | null) => (from && to ? Math.max(0, (Date.parse(to) - Date.parse(from)) / 60_000) : undefined);

function average(values: Array<number | undefined>): number | undefined {
  const known = values.filter((value): value is number => value !== undefined);
  return known.length ? Math.round((known.reduce((total, value) => total + value, 0) / known.length) * 10) / 10 : undefined;
}

/** The first day of the period a day falls in: itself, its week's Monday, or the 1st of its month. */
export function periodOf(dayText: string, granularity: Granularity): string {
  if (granularity === 'month') return `${dayText.slice(0, 7)}-01`;
  if (granularity === 'week') {
    const weekday = new Date(`${dayText}T00:00:00Z`).getUTCDay();
    return addDays(dayText, -((weekday + 6) % 7));
  }
  return dayText;
}

/** Every period of the span, so a chart shows the quiet days as zero rather than leaving them out. */
function periods(span: Span): string[] {
  const out: string[] = [];
  for (let cursor = span.from; cursor <= span.to; cursor = addDays(cursor, 1)) {
    const period = periodOf(cursor, span.granularity);
    if (out[out.length - 1] !== period) out.push(period);
  }
  return out;
}

const spanEcho = (span: Span) => ({ from: span.from, to: span.to, ...(span.branchId ? { branch_id: span.branchId } : {}) });

const appointments = () => table('appointments').where((row) => row.status !== 'draft');
const tickets = () => table('queue_tickets').all();
const consultations = () => table('consultations').all();

// ---- Today ---------------------------------------------------------------------------------------

export function todayDashboard(auth: Auth, branchIds: string[], branchId?: string): TodayDashboard {
  const today = clinicToday();
  const now = new Date().toISOString();
  const scope = { branchIds };

  const day = appointments().filter((row) => atBranch(row, scope) && clinicDayOf(row.starts_at) === today);
  const count = (...statuses: string[]) => day.filter((row) => statuses.includes(row.status)).length;

  const board = tickets().filter((row) => atBranch(row, scope) && (row.service_date ?? clinicDayOf(row.issued_at)) === today);
  const countTickets = (status: string) => board.filter((row) => row.status === status).length;
  const waiting = board.filter((row) => row.status === 'waiting');
  const oldest = waiting.map((row) => row.issued_at as string).sort()[0];

  const cases = consultations().filter((row) => atBranch(row, scope));
  const mayReadMoney = holdsAny(auth, 'analytics:read', 'invoice:write');

  const money = mayReadMoney ? cashFlow({ from: today, to: today, branchIds, granularity: 'day' }) : undefined;

  return {
    date: today,
    ...(branchId ? { branch_id: branchId } : {}),
    generated_at: now,
    appointments: {
      total: day.length,
      upcoming: count('booked', 'confirmed'),
      arrived: count('checked_in', 'in_consultation'),
      completed: count('completed'),
      cancelled: count('cancelled'),
      no_show: count('no_show'),
    },
    queue: {
      issued: board.length,
      waiting: waiting.length,
      called: countTickets('called'),
      in_room: countTickets('in_room'),
      served: countTickets('served'),
      skipped: countTickets('skipped'),
      left: countTickets('left'),
      ...optional('average_wait_minutes', average(board.map((row) => minutes(row.issued_at, row.called_at))) ),
      ...optional('longest_wait_minutes', oldest ? Math.floor(minutes(oldest, now)!) : undefined),
    },
    consultations: {
      open: cases.filter((row) => row.status === 'draft').length,
      closed_today: cases.filter((row) => row.status === 'closed' && clinicDayOf(row.closed_at) === today).length,
    },
    ...(money
      ? { revenue: { collected_satang: money.collected, refunded_satang: money.refunded, net_satang: money.collected - money.refunded, payments: money.payments } }
      : {}),
    ...(auth.staffId
      ? {
          mine: {
            appointments: day.filter((row) => row.staff_id === auth.staffId && !['cancelled', 'no_show'].includes(row.status)).length,
            queue_waiting: waiting.filter((row) => row.staff_id === auth.staffId).length,
            open_consultations: cases.filter((row) => row.status === 'draft' && row.vet_staff_id === auth.staffId).length,
          },
        }
      : {}),
  };
}

function optional<K extends string>(key: K, value: number | undefined): Partial<Record<K, number>> {
  return value === undefined ? {} : ({ [key]: value } as Record<K, number>);
}

// ---- Operations ----------------------------------------------------------------------------------

export function appointmentStats(span: Span): AppointmentStats {
  const rows = appointments().filter((row) => atBranch(row, span) && inSpan(clinicDayOf(row.starts_at), span));
  const count = (rowsIn: Row[], ...statuses: string[]) => rowsIn.filter((row) => statuses.includes(row.status)).length;
  const total = rows.length;
  const cancelled = count(rows, 'cancelled');
  const noShow = count(rows, 'no_show');

  return {
    ...spanEcho(span),
    granularity: span.granularity,
    totals: {
      total,
      upcoming: count(rows, 'booked', 'confirmed'),
      arrived: count(rows, 'checked_in', 'in_consultation'),
      completed: count(rows, 'completed'),
      cancelled,
      late_cancelled: rows.filter((row) => row.status === 'cancelled' && row.late_cancellation).length,
      no_show: noShow,
      walk_in: rows.filter((row) => row.walk_in).length,
      booked_online: rows.filter((row) => row.source === 'online').length,
      ...(total ? { cancellation_rate: cancelled / total, no_show_rate: noShow / total } : {}),
    },
    series: periods(span).map((period) => {
      const inPeriod = rows.filter((row) => periodOf(clinicDayOf(row.starts_at), span.granularity) === period);
      return { period, total: inPeriod.length, completed: count(inPeriod, 'completed'), cancelled: count(inPeriod, 'cancelled'), no_show: count(inPeriod, 'no_show') };
    }),
  };
}

export function queueStats(span: Span): QueueStats {
  const rows = tickets().filter((row) => atBranch(row, span) && inSpan(row.service_date ?? clinicDayOf(row.issued_at), span));
  const waitOf = (row: Row) => minutes(row.issued_at, row.called_at);
  const hours = new Map<number, Row[]>();
  for (const row of rows) {
    const hour = clinicHourOf(row.issued_at);
    hours.set(hour, [...(hours.get(hour) ?? []), row]);
  }
  const peak = [...hours.entries()].sort((a, b) => b[1].length - a[1].length)[0]?.[0];
  const days: string[] = [];
  for (let cursor = span.from; cursor <= span.to; cursor = addDays(cursor, 1)) days.push(cursor);

  return {
    ...spanEcho(span),
    totals: {
      tickets: rows.length,
      served: rows.filter((row) => row.status === 'served').length,
      skipped: rows.filter((row) => row.status === 'skipped').length,
      left: rows.filter((row) => row.status === 'left').length,
      outstanding: rows.filter((row) => ['waiting', 'called', 'in_room'].includes(row.status)).length,
      ...optional('average_wait_minutes', average(rows.map(waitOf))),
      ...optional('average_room_minutes', average(rows.map((row) => minutes(row.entered_room_at, row.served_at)))),
      ...optional('peak_hour', peak),
    },
    peak_hours: [...hours.entries()]
      .sort((a, b) => a[0] - b[0])
      .map(([hour, inHour]) => ({ hour, tickets: inHour.length, ...optional('average_wait_minutes', average(inHour.map(waitOf))) })),
    series: days.map((date) => {
      const inDay = rows.filter((row) => (row.service_date ?? clinicDayOf(row.issued_at)) === date);
      return { date, tickets: inDay.length, served: inDay.filter((row) => row.status === 'served').length, ...optional('average_wait_minutes', average(inDay.map(waitOf))) };
    }),
  };
}

export function workloadStats(span: Span): WorkloadStats {
  const cases = consultations().filter((row) => atBranch(row, span) && inSpan(clinicDayOf(row.opened_at), span));
  const visits = appointments().filter((row) => atBranch(row, span) && inSpan(clinicDayOf(row.starts_at), span) && row.staff_id);
  const today = clinicToday();
  const waiting = tickets().filter((row) => atBranch(row, span) && (row.service_date ?? clinicDayOf(row.issued_at)) === today && row.status === 'waiting');
  const openCases = consultations().filter((row) => atBranch(row, span) && row.status === 'draft');
  const caseMinutes = (row: Row) => (row.status === 'closed' ? minutes(row.opened_at, row.closed_at) : undefined);

  const staffIds = new Set<string>([...cases.map((row) => row.vet_staff_id), ...visits.map((row) => row.staff_id)].filter(Boolean));
  for (const member of table('staff_members').where((row) => isVet(row) && row.status === 'active')) staffIds.add(member.id);

  return {
    ...spanEcho(span),
    totals: {
      cases: cases.length,
      ...optional('average_case_minutes', average(cases.map(caseMinutes))),
      open_cases: openCases.length,
      unassigned_queue: waiting.filter((row) => !row.staff_id).length,
    },
    staff: [...staffIds]
      .map((staffId) => {
        const own = cases.filter((row) => row.vet_staff_id === staffId);
        return {
          staff_id: staffId,
          ...(staffOf(staffId)?.display_name ? { display_name: staffOf(staffId)!.display_name as string } : {}),
          cases: own.length,
          ...optional('average_case_minutes', average(own.map(caseMinutes))),
          appointments: visits.filter((row) => row.staff_id === staffId).length,
          open_cases: openCases.filter((row) => row.vet_staff_id === staffId).length,
          waiting_queue: waiting.filter((row) => row.staff_id === staffId).length,
        };
      })
      .sort((a, b) => b.cases - a.cases || b.appointments - a.appointments),
  };
}

// ---- Money ---------------------------------------------------------------------------------------

interface Flow {
  collected: number;
  refunded: number;
  payments: number;
  refunds: number;
}

/** Money on the day it moved: payments by `collected_at`, refunds by `refunded_at` (cash basis). */
function cashFlow(span: Span, invoices = liveOrRefunded(span.branchIds)): Flow & { byMethod: Map<PaymentMethod, Flow>; byBranch: Map<string, Flow>; byPeriod: Map<string, Flow> } {
  const blank = (): Flow => ({ collected: 0, refunded: 0, payments: 0, refunds: 0 });
  const total = blank();
  const byMethod = new Map<PaymentMethod, Flow>();
  const byBranch = new Map<string, Flow>();
  const byPeriod = new Map<string, Flow>();
  const bump = <K>(map: Map<K, Flow>, key: K, change: (flow: Flow) => void) => {
    const flow = map.get(key) ?? blank();
    change(flow);
    map.set(key, flow);
  };

  for (const invoice of invoices) {
    for (const payment of invoice.payments) {
      const dayText = clinicDayOf(payment.collected_at);
      if (!inSpan(dayText, span)) continue;
      const add = (flow: Flow) => {
        flow.collected += payment.amount_satang;
        flow.payments += 1;
      };
      add(total);
      bump(byMethod, payment.method, add);
      bump(byBranch, invoice.branch_id, add);
      bump(byPeriod, periodOf(dayText, span.granularity), add);
    }
    for (const refund of invoice.refunds) {
      const dayText = clinicDayOf(refund.refunded_at);
      if (!inSpan(dayText, span)) continue;
      const add = (flow: Flow) => {
        flow.refunded += refund.amount_satang;
        flow.refunds += 1;
      };
      add(total);
      bump(byMethod, refund.method, add);
      bump(byBranch, invoice.branch_id, add);
      bump(byPeriod, periodOf(dayText, span.granularity), add);
    }
  }
  return { ...total, byMethod, byBranch, byPeriod };
}

/** Bills money may have moved on: everything at the branches but a void bill (which never took any). */
function liveOrRefunded(branchIds: string[]): InvoiceRow[] {
  return liveInvoices(branchIds);
}

export function revenueAnalytics(span: Span): RevenueAnalytics {
  const flow = cashFlow(span);
  const issued = liveInvoices(span.branchIds).filter((invoice) => inSpan(clinicDayOf(invoice.issued_at), span));
  const methods: PaymentMethod[] = ['cash', 'promptpay', 'card'];

  return {
    ...spanEcho(span),
    granularity: span.granularity,
    totals: {
      collected_satang: flow.collected,
      refunded_satang: flow.refunded,
      net_satang: flow.collected - flow.refunded,
      payments: flow.payments,
      refunds: flow.refunds,
      invoiced_satang: issued.reduce((total, invoice) => total + invoice.total_satang, 0),
      invoices: issued.length,
    },
    by_method: methods.map((method) => {
      const entry = flow.byMethod.get(method) ?? { collected: 0, refunded: 0 };
      return { method, collected_satang: entry.collected, refunded_satang: entry.refunded, net_satang: entry.collected - entry.refunded };
    }),
    by_branch: span.branchIds.map((branchId) => {
      const entry = flow.byBranch.get(branchId) ?? { collected: 0, refunded: 0 };
      return {
        branch_id: branchId,
        ...(branchName(branchId) ? { branch_name: branchName(branchId) as string } : {}),
        collected_satang: entry.collected,
        refunded_satang: entry.refunded,
        net_satang: entry.collected - entry.refunded,
      };
    }),
    series: periods(span).map((period) => {
      const entry = flow.byPeriod.get(period) ?? { collected: 0, refunded: 0 };
      return { period, collected_satang: entry.collected, refunded_satang: entry.refunded, net_satang: entry.collected - entry.refunded };
    }),
  };
}

/** What each line kept of the money its bill kept: its share of the subtotal times paid less refunded. */
function keptShare(invoice: InvoiceRow, lineTotal: number): number {
  if (invoice.subtotal_satang <= 0) return 0;
  return Math.round((lineTotal / invoice.subtotal_satang) * (invoice.paid_satang - invoice.refunded_satang));
}

export function serviceAnalytics(span: Span): ServiceAnalytics {
  const issued = liveInvoices(span.branchIds).filter((invoice) => inSpan(clinicDayOf(invoice.issued_at), span));
  const services = new Map<string, { service_id?: string; name: string; quantity: number; invoices: Set<string>; billed: number; revenue: number; medicine: number }>();
  const kinds = new Map<LineKind, { billed: number; revenue: number }>();

  for (const invoice of issued) {
    const serviceLines = invoice.lines.filter((line) => line.kind === 'service');
    const serviceBilled = serviceLines.reduce((total, line) => total + line.line_total_satang, 0);
    const medicineKept = invoice.lines.filter((line) => line.kind === 'medicine').reduce((total, line) => total + keptShare(invoice, line.line_total_satang), 0);

    for (const line of invoice.lines) {
      const kept = keptShare(invoice, line.line_total_satang);
      const kind = kinds.get(line.kind) ?? { billed: 0, revenue: 0 };
      kind.billed += line.line_total_satang;
      kind.revenue += kept;
      kinds.set(line.kind, kind);

      if (line.kind !== 'service') continue;
      const service = line.source_id ? serviceOf(line.source_id) : undefined;
      const key = line.source_id ?? `name:${line.description}`;
      const entry = services.get(key) ?? {
        ...(line.source_id ? { service_id: line.source_id } : {}),
        name: (service?.name as string | undefined) ?? line.description,
        quantity: 0,
        invoices: new Set<string>(),
        billed: 0,
        revenue: 0,
        medicine: 0,
      };
      entry.quantity += line.quantity;
      entry.invoices.add(invoice.id);
      entry.billed += line.line_total_satang;
      entry.revenue += kept;
      entry.medicine += serviceBilled > 0 ? Math.round((line.line_total_satang / serviceBilled) * medicineKept) : 0;
      services.set(key, entry);
    }
  }

  const order: LineKind[] = ['service', 'treatment', 'medicine', 'other'];
  return {
    ...spanEcho(span),
    services: [...services.values()]
      .map((entry) => ({
        ...(entry.service_id ? { service_id: entry.service_id } : {}),
        name: entry.name,
        quantity: entry.quantity,
        invoices: entry.invoices.size,
        billed_satang: entry.billed,
        revenue_satang: entry.revenue,
        medicine_satang: entry.medicine,
        revenue_excluding_medicine_satang: entry.revenue - entry.medicine,
      }))
      .sort((a, b) => b.revenue_satang - a.revenue_satang),
    by_kind: order.filter((kind) => kinds.has(kind)).map((kind) => ({ kind, billed_satang: kinds.get(kind)!.billed, revenue_satang: kinds.get(kind)!.revenue })),
  };
}

/** A customer's visit months: a bill raised, or an appointment attended. */
function visitMonths(branchIds: string[]): Map<string, Set<string>> {
  const months = new Map<string, Set<string>>();
  const add = (customerId: string, iso: string) => {
    const month = `${clinicDayOf(iso).slice(0, 7)}-01`;
    const set = months.get(customerId) ?? new Set<string>();
    set.add(month);
    months.set(customerId, set);
  };
  for (const invoice of liveInvoices(branchIds)) add(invoice.customer_id, invoice.issued_at);
  for (const row of appointments()) {
    if (branchIds.includes(row.branch_id) && row.status === 'completed') add(row.customer_id, row.starts_at);
  }
  return months;
}

const monthAfter = (month: string, offset: number) => {
  const date = new Date(`${month}T00:00:00Z`);
  date.setUTCMonth(date.getUTCMonth() + offset);
  return date.toISOString().slice(0, 10);
};

export function retentionAnalytics(span: Span): RetentionAnalytics {
  const visits = visitMonths(span.branchIds);
  const first = new Map([...visits.entries()].map(([customerId, months]) => [customerId, [...months].sort()[0]!]));
  const fromMonth = `${span.from.slice(0, 7)}-01`;
  const toMonth = `${span.to.slice(0, 7)}-01`;
  const currentMonth = `${clinicToday().slice(0, 7)}-01`;

  const months: string[] = [];
  for (let month = fromMonth; month <= toMonth; month = monthAfter(month, 1)) months.push(month);

  return {
    ...spanEcho(span),
    months: months.map((month) => {
      const active = [...visits.entries()].filter(([, set]) => set.has(month)).map(([customerId]) => customerId);
      const fresh = active.filter((customerId) => first.get(customerId) === month).length;
      return { month, active: active.length, new: fresh, returning: active.length - fresh };
    }),
    cohorts: months.map((cohort) => {
      const members = [...first.entries()].filter(([, month]) => month === cohort).map(([customerId]) => customerId);
      const retained: number[] = [];
      const rates: Array<number | null> = [];
      for (let offset = 0; monthAfter(cohort, offset) <= currentMonth; offset += 1) {
        const month = monthAfter(cohort, offset);
        const back = members.filter((customerId) => visits.get(customerId)!.has(month)).length;
        retained.push(back);
        rates.push(members.length ? back / members.length : null);
      }
      return { cohort, customers: members.length, retained, retention_rates: rates };
    }),
  };
}

export function staffPerformance(span: Span): StaffPerformance {
  const cases = consultations().filter((row) => atBranch(row, span) && row.status === 'closed' && inSpan(clinicDayOf(row.closed_at), span));
  const bills = liveInvoices(span.branchIds);
  const billOfCase = new Map(bills.filter((invoice) => invoice.consultation_id).map((invoice) => [invoice.consultation_id!, invoice]));
  const billOfVisit = new Map(bills.filter((invoice) => invoice.appointment_id).map((invoice) => [invoice.appointment_id!, invoice]));
  const visits = appointments().filter((row) => atBranch(row, span) && row.status === 'completed' && inSpan(clinicDayOf(row.starts_at), span) && row.staff_id);

  const members = table('staff_members').where((row) => row.status === 'active' && (isVet(row) || !!(row.roles as string[] | undefined)?.length));
  return {
    ...spanEcho(span),
    staff: members
      .map((member) => {
        const own = cases.filter((row) => row.vet_staff_id === member.id);
        // A visit with no case on file still counts for its vet: the bill raised for it is theirs.
        const ownVisits = visits.filter((row) => row.staff_id === member.id && !own.some((entry) => entry.appointment_id === row.id));
        const attributed = [
          ...own.map((row) => billOfCase.get(row.id) ?? (row.appointment_id ? billOfVisit.get(row.appointment_id) : undefined)),
          ...ownVisits.map((row) => billOfVisit.get(row.id)),
        ].reduce((total, invoice) => total + (invoice ? invoice.paid_satang - invoice.refunded_satang : 0), 0);
        const collected = bills
          .flatMap((invoice) => invoice.payments)
          .filter((payment) => payment.collected_by_staff_id === member.id && inSpan(clinicDayOf(payment.collected_at), span))
          .reduce((total, payment) => total + payment.amount_satang, 0);
        const treatments = own.reduce((total, row) => total + ((row.treatments as Row[] | undefined)?.filter((entry) => entry.performed_by_staff_id === member.id).length ?? 0), 0);
        return {
          staff_id: member.id,
          display_name: member.display_name as string,
          ...(member.staff_type ? { staff_type: member.staff_type as string } : {}),
          cases: own.length + ownVisits.length,
          ...optional('average_case_minutes', average(own.map((row) => minutes(row.opened_at, row.closed_at)))),
          treatments,
          attributed_revenue_satang: attributed,
          collected_satang: collected,
        };
      })
      .filter((row) => row.cases > 0 || row.collected_satang > 0)
      .sort((a, b) => b.attributed_revenue_satang - a.attributed_revenue_satang || b.collected_satang - a.collected_satang),
  };
}
