import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listSegments } from '@/features/crm/api/segments';
import { SegmentForm } from '@/features/crm/components/segments/segment-form';
import { SegmentList } from '@/features/crm/components/segments/segment-list';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { segmentListText } from '@/features/crm/content/segment-list';
import { segmentPage } from '@/features/crm/content/segment-page';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { readCursor, withCursor } from '@/features/crm/lib/read-list-query';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: crmArea.metaTitle(segmentPage.title) };

/**
 * Customer groups: the form to save one (`crm:manage`) and the groups saved before (`crm:read`,
 * docs/contract-extensions.md § 25). Opening a group counts it again and offers the broadcast.
 */
export default async function CrmSegmentsPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, params] = await Promise.all([loadStaffContext(), searchParams]);
  const canManage = Boolean(context?.can(Scope.CrmManage));
  const canRead = Boolean(context?.can(Scope.CrmRead));

  if (!context || (!canManage && !canRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }

  const cursor = readCursor(params);
  const page = canRead ? await authed(() => listSegments(cursor)) : null;

  return (
    <div className="space-y-5">
      <PageHeader title={segmentPage.title} subtitle={segmentPage.subtitle} />
      {canManage && (
        <Card className="max-w-3xl">
          <CardBody>
            <SegmentForm
              branches={context.branches.map(({ id, name }) => ({ id, name }))}
              canBroadcast={context.can(Scope.NotifyBroadcast)}
            />
          </CardBody>
        </Card>
      )}
      {page ? (
        <SegmentList
          segments={page.data}
          nextHref={page.page.next_cursor ? withCursor(crmPaths.segments, {}, page.page.next_cursor) : undefined}
          firstHref={cursor ? crmPaths.segments : undefined}
        />
      ) : (
        <p className="text-sm text-ink-600">{segmentListText.noRead}</p>
      )}
    </div>
  );
}
