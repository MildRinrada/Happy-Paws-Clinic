import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { LineBroadcastForm } from '@/features/integrations/components/line-broadcast-form';
import { lineBroadcastPage } from '@/features/integrations/content/line-broadcast';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: lineBroadcastPage.metaTitle };

/** An announcement on LINE to customers picked one by one. */
export default async function LineBroadcastPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.NotifyBroadcast)) {
    return <Alert tone="info">{lineBroadcastPage.noAccess}</Alert>;
  }

  return (
    <div className="space-y-6">
      <PageHeader title={lineBroadcastPage.title} subtitle={lineBroadcastPage.subtitle} />
      <LineBroadcastForm canSearchCustomers={context.can(Scope.CustomerRead)} />
    </div>
  );
}
