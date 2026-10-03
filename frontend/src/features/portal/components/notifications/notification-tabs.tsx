import Link from 'next/link';
import { portalPaths } from '../../content/portal-navigation';
import { portalNotifications } from '../../content/portal-notifications';

/** "ทั้งหมด" and "ยังไม่อ่าน" as two links; the one in view is filled. */
export function NotificationTabs({ unreadOnly, unreadCount }: { unreadOnly: boolean; unreadCount: number }) {
  const tabs = [
    { label: portalNotifications.tabs.all, href: portalPaths.notifications, current: !unreadOnly },
    { label: portalNotifications.tabs.unread(unreadCount), href: `${portalPaths.notifications}?unread=true`, current: unreadOnly },
  ];

  return (
    <nav className="flex gap-2">
      {tabs.map((tab) => (
        <Link
          key={tab.href}
          href={tab.href}
          aria-current={tab.current ? 'page' : undefined}
          className={`inline-flex h-9 items-center rounded-full px-4 text-sm font-medium ${
            tab.current ? 'bg-site-teal text-white' : 'bg-white text-ink-700 shadow-sm hover:text-site-teal'
          }`}
        >
          {tab.label}
        </Link>
      ))}
    </nav>
  );
}
