import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { LineBroadcastForm } from '@/features/integrations/components/line-broadcast-form';
import { lineBroadcastPage } from '@/features/integrations/content/line-broadcast';
import { readBroadcastSegment } from '@/features/integrations/lib/broadcast-segment-param';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: lineBroadcastPage.metaTitle };

/**
 * An announcement on LINE to customers picked one by one, or to a CRM segment when the page is
 * opened from `/crm/segments` with `?segment_id=`.
 */
export default async function LineBroadcastPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | string[] | undefined>>;
}) {
  const [context, query] = await Promise.all([loadStaffContext(), searchParams]);

  if (!context?.can(Scope.NotifyBroadcast)) {
    return <Alert tone="info">{lineBroadcastPage.noAccess}</Alert>;
  }

  return (
    <div className="space-y-6">
      <PageHeader title={lineBroadcastPage.title} subtitle={lineBroadcastPage.subtitle} />
      <LineBroadcastForm canSearchCustomers={context.can(Scope.CustomerRead)} segment={readBroadcastSegment(query)} />
    </div>
  );
}
