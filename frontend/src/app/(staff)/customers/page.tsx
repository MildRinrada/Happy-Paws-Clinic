import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { searchCustomers } from '@/features/customers/api/searchCustomers';
import { CustomerSearchForm } from '@/features/customers/components/customer-search-form';
import { CustomerTable } from '@/features/customers/components/customer-table';
import { customerList } from '@/features/customers/content/customer-list';
import { customerPaths } from '@/features/customers/lib/customer-paths';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: customerList.metaTitle };

/**
 * The customer register at the counter: one search box, the matches, and the way to add someone
 * new. With nothing typed it lists the newest registrations.
 */
export default async function CustomersPage({
  searchParams,
}: {
  searchParams: Promise<{ q?: string; cursor?: string }>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);
  const text = customerList;

  if (!context?.can(Scope.CustomerRead)) {
    return <Alert tone="info">{text.noAccess}</Alert>;
  }

  const query = (params.q ?? '').trim().slice(0, 100);

  let page;
  try {
    page = await authed(() => searchCustomers({ q: query, cursor: params.cursor }));
  } catch (error) {
    if (error instanceof ApiError && (error.status === 403 || error.status === 422)) {
      return <Alert>{error.message}</Alert>;
    }
    throw error;
  }

  const nextCursor = page.page.next_cursor;
  const nextHref = nextCursor
    ? `${customerPaths.list}?${new URLSearchParams({ ...(query && { q: query }), cursor: nextCursor })}`
    : undefined;

  return (
    <div className="space-y-6">
      <PageHeader
        title={text.title}
        subtitle={text.subtitle}
        actions={
          context.can(Scope.CustomerWrite) && (
            <Link href={customerPaths.register} className={buttonClass()}>
              {text.register}
            </Link>
          )
        }
      />

      <CustomerSearchForm query={query} />

      <section className="space-y-3">
        <h2 className="text-sm font-medium text-ink-500">
          {query ? text.resultsHeading(query) : text.recentHeading}
        </h2>

        {page.data.length === 0 ? (
          <EmptyState title={text.empty.title}>{query ? text.empty.search : text.empty.none}</EmptyState>
        ) : (
          <CustomerTable customers={page.data} />
        )}

        {nextHref && (
          <Link href={nextHref} className={buttonClass('secondary', 'sm')}>
            {text.next}
          </Link>
        )}
      </section>
    </div>
  );
}
