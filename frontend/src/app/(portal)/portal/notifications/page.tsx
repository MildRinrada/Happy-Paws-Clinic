import type { Metadata } from 'next';
import { listMyNotifications } from '@/features/portal/api/notifications';
import { MarkReadButton } from '@/features/portal/components/notifications/mark-read-button';
import { NotificationItem } from '@/features/portal/components/notifications/notification-item';
import { NotificationPager } from '@/features/portal/components/notifications/notification-pager';
import { NotificationTabs } from '@/features/portal/components/notifications/notification-tabs';
import { EmptyLine } from '@/features/portal/components/shared/empty-line';
import { PageHeading } from '@/features/portal/components/shared/page-heading';
import { portalNotifications } from '@/features/portal/content/portal-notifications';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: `${portalNotifications.title} · Happy Paws Clinic` };

interface NotificationsPageProps {
  searchParams: Promise<{ unread?: string; cursor?: string }>;
}

/** The owner's inbox: everything the clinic has sent them, newest first, 20 to a page. */
export default async function PortalNotificationsPage({ searchParams }: NotificationsPageProps) {
  const { unread, cursor } = await searchParams;
  const unreadOnly = unread === 'true';
  const inbox = await authed(() => listMyNotifications(unreadOnly, cursor));
  const text = portalNotifications;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <PageHeading title={text.title} intro={text.intro} />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <NotificationTabs unreadOnly={unreadOnly} unreadCount={inbox.unread_count} />
        {inbox.unread_count > 0 && <MarkReadButton label={text.markAll} variant="button" />}
      </div>

      {inbox.data.length === 0 ? (
        <EmptyLine>{unreadOnly ? text.emptyUnread : text.empty}</EmptyLine>
      ) : (
        <ul className="divide-y divide-ink-100 overflow-hidden rounded-2xl bg-white shadow-sm">
          {inbox.data.map((item) => (
            <NotificationItem key={item.id} item={item} />
          ))}
        </ul>
      )}

      <NotificationPager unreadOnly={unreadOnly} nextCursor={inbox.page.next_cursor} paged={Boolean(cursor)} />
    </div>
  );
}
