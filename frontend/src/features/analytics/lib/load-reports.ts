import { optional } from '@/lib/api/api-error';
import { getAppointmentStats } from '../api/getAppointmentStats';
import { getQueueStats } from '../api/getQueueStats';
import { getRetentionAnalytics } from '../api/getRetentionAnalytics';
import { getRevenueAnalytics } from '../api/getRevenueAnalytics';
import { getServiceAnalytics } from '../api/getServiceAnalytics';
import { getStaffPerformance } from '../api/getStaffPerformance';
import { getWorkloadStats } from '../api/getWorkloadStats';
import type {
  AppointmentStats,
  QueueStats,
  ReportQuery,
  RetentionAnalytics,
  RevenueAnalytics,
  ServiceAnalytics,
  StaffPerformance,
  WorkloadStats,
} from '../types/analytics.types';
import type { ReportRange } from './report-range';

/** A report the API refused (403) comes back as `null` and the tab says so instead of failing whole. */
export interface OperationsReports {
  appointments: AppointmentStats | null;
  queue: QueueStats | null;
  workload: WorkloadStats | null;
}

export interface BusinessReports {
  revenue: RevenueAnalytics | null;
  services: ServiceAnalytics | null;
  retention: RetentionAnalytics | null;
  /** `undefined` when the session does not hold `analytics:hr` and it was not asked for. */
  staff?: StaffPerformance | null;
}

function queryOf(range: ReportRange, branchId: string): ReportQuery {
  return { from: range.from, to: range.to, branchId, granularity: range.granularity };
}

/** The three operational reports for one range, read side by side. */
export async function loadOperationsReports(range: ReportRange, branchId: string): Promise<OperationsReports> {
  const query = queryOf(range, branchId);
  const [appointments, queue, workload] = await Promise.all([
    optional(getAppointmentStats(query)),
    optional(getQueueStats(query)),
    optional(getWorkloadStats(query)),
  ]);

  return { appointments, queue, workload };
}

/** The business reports the session may read. Staff performance is a separate scope. */
export async function loadBusinessReports(
  range: ReportRange,
  branchId: string,
  access: { read: boolean; hr: boolean },
): Promise<BusinessReports> {
  const query = queryOf(range, branchId);
  const skip = Promise.resolve(null);
  const [revenue, services, retention, staff] = await Promise.all([
    access.read ? optional(getRevenueAnalytics(query)) : skip,
    access.read ? optional(getServiceAnalytics(query)) : skip,
    access.read ? optional(getRetentionAnalytics(query)) : skip,
    access.hr ? optional(getStaffPerformance(query)) : Promise.resolve(undefined),
  ]);

  return { revenue, services, retention, staff };
}
