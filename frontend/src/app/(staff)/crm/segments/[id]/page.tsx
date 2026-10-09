import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { getSegment } from '@/features/crm/api/segments';
import { SegmentDetail } from '@/features/crm/components/segments/segment-detail';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { segmentListText } from '@/features/crm/content/segment-list';
import { segmentPage } from '@/features/crm/content/segment-page';
import { crmPaths } from '@/features/crm/lib/crm-paths';
import { ApiError } from '@/lib/api/api-error';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: crmArea.metaTitle(segmentPage.title) };

/** One saved group, counted again now (docs/contract-extensions.md § 25), with the broadcast link. */
export default async function CrmSegmentPage({ params }: { params: Promise<{ id: string }> }) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.CrmRead)) {
    return <Alert tone="info">{crmArea.noAccess}</Alert>;
  }

  const { id } = await params;
  const text = segmentListText.detail;
  const segment = await authed(() => getSegment(id)).catch((error: unknown) => {
    if (error instanceof ApiError && (error.status === 404 || error.status === 403)) return error.status;
    throw error;
  });

  return (
    <div className="max-w-3xl space-y-5">
      <BackLink href={crmPaths.segments}>{text.back}</BackLink>
      {segment === 404 && <Alert>{text.notFound}</Alert>}
      {segment === 403 && <Alert>{text.refused}</Alert>}
      {typeof segment === 'object' && (
        <SegmentDetail
          segment={segment}
          canBroadcast={context.can(Scope.NotifyBroadcast)}
          branchName={context.branches.find((branch) => branch.id === segment.criteria.branch_id)?.name}
        />
      )}
    </div>
  );
}
