import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getDeliveryLog } from '@/features/notifications/api/getDeliveryLog';
import { DeliveryLogFilters } from '@/features/notifications/components/delivery-log-filters';
import { DeliveryLogTable } from '@/features/notifications/components/delivery-log-table';
import { deliveryLogPage } from '@/features/notifications/content/delivery-log';
import { LOG_PATH, nextPageHref, readLogQuery } from '@/features/notifications/lib/delivery-log-query';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: deliveryLogPage.metaTitle };

interface DeliveryLogPageProps {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}

/** Every attempt to reach a customer, newest first, filterable by result, channel and source. */
export default async function DeliveryLogPage({ searchParams }: DeliveryLogPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.NotifyRead)) {
    return <Alert tone="info">{deliveryLogPage.noAccess}</Alert>;
  }

  const query = readLogQuery(await searchParams);
  const page = await authed(() => getDeliveryLog(query));
  const next = page.page.next_cursor;

  return (
    <div className="space-y-5">
      <PageHeader title={deliveryLogPage.title} subtitle={deliveryLogPage.subtitle} />
      <DeliveryLogFilters query={query} basePath={LOG_PATH} />

      {page.data.length === 0 ? <EmptyState title={deliveryLogPage.empty} /> : <DeliveryLogTable rows={page.data} />}

      {next && (
        <Link href={nextPageHref(query, next)} className={buttonClass('secondary', 'sm')}>
          {deliveryLogPage.next}
        </Link>
      )}
    </div>
  );
}
