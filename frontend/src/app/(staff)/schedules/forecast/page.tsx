import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getDemandForecast } from '@/features/schedules/api/getDemandForecast';
import { DemandDayCard } from '@/features/schedules/components/forecast/demand-day-card';
import { DemandRangeForm } from '@/features/schedules/components/forecast/demand-range-form';
import { demandForecastPage } from '@/features/schedules/content/forecast-page';
import { readForecastRange } from '@/features/schedules/lib/forecast-range';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';
import { clinicDay } from '@/lib/format/datetime';

export const metadata: Metadata = { title: demandForecastPage.metaTitle };

interface DemandForecastPageProps {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * Hour by hour for the days ahead at the branch in view: bookings, expected walk-ins, vets on the
 * roster, and the hours that look heavy (docs/contract-extensions.md § 33). On `stats:read`.
 */
export default async function DemandForecastPage({ searchParams }: DemandForecastPageProps) {
  const context = await loadStaffContext();
  const text = demandForecastPage;

  if (!context?.can(Scope.StatsRead)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{text.noBranch}</Alert>;
  }

  const branch = context.branch;
  const today = clinicDay(new Date());
  const range = readForecastRange(await searchParams, today);

  let forecast;
  try {
    forecast = await authed(() => getDemandForecast(branch.id, range.from, range.to));
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  return (
    <div className="max-w-4xl space-y-5">
      <PageHeader
        title={text.title}
        subtitle={text.subtitle(branch.name)}
        actions={
          context.can(Scope.ScheduleRead) && (
            <Link href="/schedules" className={buttonClass('secondary', 'sm')}>
              {text.back}
            </Link>
          )
        }
      />
      <DemandRangeForm from={range.from} days={range.days} today={today} />
      <div className="space-y-1 text-sm text-ink-600">
        <p>{text.how(forecast.history_weeks)}</p>
        <p>{forecast.high_load_per_vet !== undefined ? text.line(forecast.high_load_per_vet) : text.noLine}</p>
      </div>
      <div className="space-y-4">
        {forecast.days.map((day) => (
          <DemandDayCard key={day.date} day={day} />
        ))}
      </div>
    </div>
  );
}
