import { redirect } from 'next/navigation';
import { Alert } from '@/components/ui/alert';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { notificationsArea, notificationTabs } from '@/features/notifications/content/notification-tabs';

/** `/notifications` itself has nothing of its own: it opens the first page this session can use. */
export default async function NotificationsIndexPage() {
  const context = await loadStaffContext();
  const first = notificationTabs.find((tab) => context?.can(tab.scope));

  if (first) {
    redirect(first.href);
  }

  return <Alert tone="info">{notificationsArea.noAccess}</Alert>;
}
