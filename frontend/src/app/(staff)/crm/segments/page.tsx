import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { SegmentForm } from '@/features/crm/components/segments/segment-form';
import { crmArea } from '@/features/crm/content/crm-tabs';
import { segmentPage } from '@/features/crm/content/segment-page';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: crmArea.metaTitle(segmentPage.title) };

/**
 * A group of customers picked by conditions. There is no list of saved segments in the contract,
 * so the answer is shown right away and its id travels on in the broadcast link.
 */
export default async function CrmSegmentsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.CrmManage)) {
    return <Alert tone="info">{crmArea.noManage}</Alert>;
  }

  return (
    <div className="max-w-3xl space-y-5">
      <PageHeader title={segmentPage.title} subtitle={segmentPage.subtitle} />
      <Card>
        <CardBody>
          <SegmentForm
            branches={context.branches.map(({ id, name }) => ({ id, name }))}
            canBroadcast={context.can(Scope.NotifyBroadcast)}
          />
        </CardBody>
      </Card>
    </div>
  );
}
