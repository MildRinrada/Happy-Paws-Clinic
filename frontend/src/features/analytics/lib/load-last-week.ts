import { getAppointmentStats } from '../api/getAppointmentStats';
import { getRevenueAnalytics } from '../api/getRevenueAnalytics';
import type { LastWeek } from '../types/today.types';
import { shiftDay } from './report-range';
import { tolerant } from './tolerant-read';

/**
 * The same weekday last week, read from the range reports as a one-day range: the day's
 * appointments on `stats:read`, its net takings on `analytics:read`. A Monday is compared with a
 * Monday because clinic days differ by weekday far more than week to week. Either figure is simply
 * absent when the session may not read it or the read fails; the page then shows no comparison.
 */
export async function loadLastWeek(
  branchId: string,
  today: string,
  access: { appointments: boolean; revenue: boolean },
): Promise<LastWeek> {
  const day = shiftDay(today, -7);
  const query = { from: day, to: day, branchId };

  const [appointments, revenue] = await Promise.all([
    access.appointments ? tolerant(() => getAppointmentStats(query)) : Promise.resolve(null),
    access.revenue ? tolerant(() => getRevenueAnalytics(query)) : Promise.resolve(null),
  ]);

  return {
    day,
    appointments: appointments?.totals.total,
    revenueSatang: revenue?.totals.net_satang,
  };
}
