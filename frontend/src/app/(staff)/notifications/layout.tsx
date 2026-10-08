import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { NotificationTabBar } from '@/features/notifications/components/notification-tab-bar';
import { notificationTabs } from '@/features/notifications/content/notification-tabs';

/** Every notification page sits under one row of tabs, showing only the pages this session can use. */
export default async function NotificationsLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();
  const tabs = notificationTabs.filter((tab) => context?.can(tab.scope));

  return (
    <div className="max-w-5xl space-y-6">
      {tabs.length > 0 && <NotificationTabBar tabs={tabs} />}
      {children}
    </div>
  );
}
