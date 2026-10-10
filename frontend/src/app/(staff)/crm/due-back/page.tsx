import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { buttonClass } from '@/components/ui/button';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listDueBack } from '@/features/crm/api/listDueBack';
import { DueBackList } from '@/features/crm/components/due-back/due-back-list';
import { DueBackReasonTabs } from '@/features/crm/components/due-back/due-back-reason-tabs';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { dueBackPage, dueBackReasons } from '@/features/crm/content/due-back-page';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { readDueBackReason } from '@/features/crm/lib/due-back-query';
import { readCursor } from '@/features/crm/lib/read-list-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: crmArea.metaTitle(dueBackPage.title) };

/**
 * Customers to call at the branch in view: pets whose vaccine is overdue, or customers later back
 * than their own usual gap, with nothing booked (docs/contract-extensions.md § 33). On `crm:read`.
 */
export default async function DueBackPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.CrmRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }
  if (!context.branch) {
    return <Alert tone="info">{crmArea.noBranch}</Alert>;
  }

  const branch = context.branch;
  const reason = readDueBackReason(params);
  const cursor = readCursor(params);
  const page = await authed(() => listDueBack(reason, branch.id, cursor));

  return (
    <div className="space-y-5">
      <PageHeader
        title={dueBackPage.title}
        subtitle={dueBackPage.subtitle(branch.name)}
        actions={
          context.can(Scope.NotifySend) && (
            <Link href="/notifications/send" className={buttonClass('secondary', 'sm')}>
              {dueBackPage.send}
            </Link>
          )
        }
      />
      <DueBackReasonTabs current={reason} />
      <p className="text-sm text-ink-600">{dueBackReasons[reason].hint}</p>
      <DueBackList
        rows={page.data}
        nextHref={page.page.next_cursor ? crmPaths.dueBack(reason, page.page.next_cursor) : undefined}
        firstHref={cursor ? crmPaths.dueBack(reason) : undefined}
      />
    </div>
  );
}
