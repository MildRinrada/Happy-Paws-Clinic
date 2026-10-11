import { portalHome } from '../../content/portal-home';
import { formatSentAt } from '../../lib/home-dates';
import type { InboxItem } from '../../types/portal.types';

/**
 * One message on the home page: "ใหม่" while unread, its title, and when it came. The whole text
 * is on the notifications page, one tap away through "ดูทั้งหมด".
 */
export function HomeNotificationRow({ item }: { item: InboxItem }) {
  const text = portalHome.notifications;

  return (
    <li className="flex items-start gap-3 py-3.5">
      {!item.read_at && (
        <span className="mt-px shrink-0 rounded-full bg-brand-600 px-2 font-display text-xs font-semibold leading-5 text-white">
          {text.unreadMark}
        </span>
      )}
      <div className="min-w-0">
        <p className="break-words text-sm leading-[22px] text-ink-900">{item.title}</p>
        <p className="text-xs leading-5 text-ink-600">{formatSentAt(item.sent_at)}</p>
      </div>
    </li>
  );
}
