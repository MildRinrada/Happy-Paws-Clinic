import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { SendNotificationForm } from '@/features/notifications/components/send-notification-form';
import { sendPage } from '@/features/notifications/content/send-page';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: sendPage.metaTitle };

/** Sending a reminder, a vaccine notice or a note to one owner or several. */
export default async function SendNotificationPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.NotifySend)) {
    return <Alert tone="info">{sendPage.noAccess}</Alert>;
  }

  return (
    <div className="space-y-6">
      <PageHeader title={sendPage.title} subtitle={sendPage.subtitle} />
      <SendNotificationForm canSearchCustomers={context.can(Scope.CustomerRead)} />
    </div>
  );
}
