/**
 * The shapes of the statistics and analytics reads. Field names are the API's (snake_case); money
 * is satang; days are clinic days as `YYYY-MM-DD`. A field the API has nothing for is absent, not
 * null, so the optional ones are marked `?`.
 */

export type Granularity = 'day' | 'week' | 'month';

/** `GET /staff/dashboard/today`. */
export interface TodayDashboard {
  date: string;
  branch_id?: string;
  generated_at: string;
  appointments: {
    total: number;
    upcoming: number;
    arrived: number;
    completed: number;
    cancelled: number;
    no_show: number;
  };
  queue: {
    issued: number;
    waiting: number;
    called: number;
    in_room: number;
    served: number;
    skipped: number;
    left: number;
    average_wait_minutes?: number;
    longest_wait_minutes?: number;
  };
  consultations: { open: number; closed_today: number };
  /** Only for a caller holding `analytics:read` or `invoice:write`. */
  revenue?: { collected_satang: number; refunded_satang: number; net_satang: number; payments: number };
  mine?: { appointments: number; queue_waiting: number; open_consultations: number };
}

interface ReportSpan {
  from: string;
  to: string;
  branch_id?: string;
}

/** `GET /staff/stats/appointments`. */
export interface AppointmentStats extends ReportSpan {
  granularity: Granularity;
  totals: {
    total: number;
    upcoming: number;
    arrived: number;
    completed: number;
    cancelled: number;
    late_cancelled: number;
    no_show: number;
    walk_in: number;
    booked_online: number;
    cancellation_rate?: number;
    no_show_rate?: number;
  };
  series: { period: string; total: number; completed: number; cancelled: number; no_show: number }[];
}

/** `GET /staff/stats/queue`. */
export interface QueueStats extends ReportSpan {
  totals: {
    tickets: number;
    served: number;
    skipped: number;
    left: number;
    outstanding: number;
    average_wait_minutes?: number;
    average_room_minutes?: number;
    peak_hour?: number;
  };
  peak_hours: { hour: number; tickets: number; average_wait_minutes?: number }[];
  series: { date: string; tickets: number; served: number; average_wait_minutes?: number }[];
}

/** `GET /staff/stats/workload`. */
export interface WorkloadStats extends ReportSpan {
  totals: { cases: number; average_case_minutes?: number; open_cases: number; unassigned_queue: number };
  staff: {
    staff_id: string;
    display_name?: string;
    cases: number;
    average_case_minutes?: number;
    appointments: number;
    open_cases: number;
    waiting_queue: number;
  }[];
}

export type PaymentMethod = 'cash' | 'card' | 'promptpay';

/** `GET /staff/analytics/revenue`. Cash basis: money on the day it moved. */
export interface RevenueAnalytics extends ReportSpan {
  granularity: Granularity;
  totals: {
    collected_satang: number;
    refunded_satang: number;
    net_satang: number;
    payments: number;
    refunds: number;
    invoiced_satang: number;
    invoices: number;
  };
  by_method: { method: PaymentMethod; collected_satang: number; refunded_satang: number; net_satang: number }[];
  by_branch: {
    branch_id: string;
    branch_name?: string;
    collected_satang: number;
    refunded_satang: number;
    net_satang: number;
  }[];
  series: { period: string; collected_satang: number; refunded_satang: number; net_satang: number }[];
}

export type LineKind = 'service' | 'treatment' | 'medicine' | 'other';

/** `GET /staff/analytics/services`. */
export interface ServiceAnalytics extends ReportSpan {
  services: {
    service_id?: string;
    name: string;
    quantity: number;
    invoices: number;
    billed_satang: number;
    revenue_satang: number;
    medicine_satang: number;
    revenue_excluding_medicine_satang: number;
  }[];
  by_kind: { kind: LineKind; billed_satang: number; revenue_satang: number }[];
}

/** `GET /staff/analytics/retention`. */
export interface RetentionAnalytics extends ReportSpan {
  months: { month: string; active: number; new: number; returning: number }[];
  cohorts: { cohort: string; customers: number; retained: number[]; retention_rates: (number | null)[] }[];
}

/** `GET /staff/analytics/staff-performance`. */
export interface StaffPerformance extends ReportSpan {
  staff: {
    staff_id: string;
    display_name: string;
    staff_type?: string;
    cases: number;
    average_case_minutes?: number;
    treatments: number;
    attributed_revenue_satang: number;
    collected_satang: number;
  }[];
}

/** What every ranged read is asked for. */
export interface ReportQuery {
  from: string;
  to: string;
  branchId?: string;
  granularity?: Granularity;
}
