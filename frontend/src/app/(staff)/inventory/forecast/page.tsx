import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getInventoryForecast } from '@/features/inventory/api/getInventoryForecast';
import { BranchNote } from '@/features/inventory/components/branch-note';
import { ForecastList } from '@/features/inventory/components/forecast/forecast-list';
import { ForecastOptionsForm } from '@/features/inventory/components/forecast/forecast-options-form';
import { forecastPage } from '@/features/inventory/content/forecast-page';
import { inventoryArea } from '@/features/inventory/content/inventory-tabs';
import { readForecastQuery, sortForecast } from '@/features/inventory/lib/forecast-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: forecastPage.metaTitle };

interface ForecastPageProps {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/**
 * "ยาจะหมดเมื่อไรและควรสั่งเท่าไร" for the branch in view, worked out from its own ledger
 * (docs/contract-extensions.md § 33). Soonest out first unless another order is chosen.
 */
export default async function InventoryForecastPage({ searchParams }: ForecastPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.InventoryRead)) {
    return <Alert tone="info">{forecastPage.noAccess}</Alert>;
  }

  if (!context.branch) {
    return <Alert tone="info">{inventoryArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const { coverDays, sort } = readForecastQuery(await searchParams);
  const forecast = await authed(() => getInventoryForecast(branch.id, coverDays));
  const rows = sortForecast(forecast.data, sort);

  return (
    <div className="space-y-5">
      <PageHeader title={forecastPage.title} subtitle={forecastPage.subtitle} actions={<BranchNote name={branch.name} />} />
      <ForecastOptionsForm coverDays={coverDays} sort={sort} />
      <p className="text-sm text-ink-600">{forecastPage.how(forecast.lead_time_days, forecast.cover_days)}</p>
      {rows.length === 0 ? (
        <EmptyState title={forecastPage.empty} />
      ) : (
        <ForecastList rows={rows} leadTimeDays={forecast.lead_time_days} />
      )}
    </div>
  );
}
