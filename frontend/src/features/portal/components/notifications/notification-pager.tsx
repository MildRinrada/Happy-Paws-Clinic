import Link from 'next/link';
import { portalPaths } from '../../content/portal-navigation';
import { portalNotifications } from '../../content/portal-notifications';

interface NotificationPagerProps {
  unreadOnly: boolean;
  nextCursor: string | null;
  /** True when this is not the first page, so there is a newest page to go back to. */
  paged: boolean;
}

/**
 * Older messages and back to the newest. The API pages forward only (a cursor to the next page),
 * so there is no "previous" step, only a return to the start.
 */
export function NotificationPager({ unreadOnly, nextCursor, paged }: NotificationPagerProps) {
  const base = unreadOnly ? `${portalPaths.notifications}?unread=true` : portalPaths.notifications;
  const join = unreadOnly ? '&' : '?';

  if (!nextCursor && !paged) return null;

  return (
    <div className="flex justify-between gap-4 text-sm font-semibold">
      {paged ? (
        <Link href={base} className="text-site-teal hover:underline">
          {portalNotifications.newest}
        </Link>
      ) : (
        <span />
      )}
      {nextCursor && (
        <Link href={`${base}${join}cursor=${encodeURIComponent(nextCursor)}`} className="text-site-teal hover:underline">
          {portalNotifications.older}
        </Link>
      )}
    </div>
  );
}
