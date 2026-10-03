import { formatDateTime } from '@/lib/format/datetime';
import { portalNotifications } from '../../content/portal-notifications';
import type { InboxItem } from '../../types/portal.types';
import { MarkReadButton } from './mark-read-button';

/** One message: an unread one is tinted and has its own "อ่านแล้ว" button. */
export function NotificationItem({ item }: { item: InboxItem }) {
  const unread = !item.read_at;

  return (
    <li className={`flex gap-3 px-5 py-4 ${unread ? 'bg-brand-50/60' : ''}`}>
      <span
        aria-hidden="true"
        className={`mt-1.5 size-2 shrink-0 rounded-full ${unread ? 'bg-site-alert' : 'bg-transparent'}`}
      />
      <div className="min-w-0 flex-1">
        <p className="text-sm font-semibold text-ink-900">
          {unread && <span className="sr-only">{portalNotifications.unreadMark} </span>}
          {item.title}
        </p>
        <p className="mt-0.5 text-sm text-ink-700">{item.body}</p>
        <div className="mt-1 flex flex-wrap items-center gap-3">
          <span className="text-xs text-site-muted">{formatDateTime(item.sent_at)}</span>
          {unread && <MarkReadButton ids={[item.id]} label={portalNotifications.markOne} />}
        </div>
      </div>
    </li>
  );
}
