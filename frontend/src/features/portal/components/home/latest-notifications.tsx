import { formatDateTime } from '@/lib/format/datetime';
import { portalHome } from '../../content/portal-home';
import { portalPaths } from '../../content/portal-navigation';
import type { InboxItem } from '../../types/portal.types';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';

/** The three newest messages from the clinic, and the way to the whole inbox. */
export function LatestNotifications({ items }: { items: InboxItem[] }) {
  const text = portalHome.notifications;

  return (
    <SectionCard title={text.title} more={{ label: text.more, href: portalPaths.notifications }}>
      {items.length === 0 ? (
        <EmptyLine>{text.empty}</EmptyLine>
      ) : (
        <ul className="divide-y divide-ink-100">
          {items.map((item) => (
            <li key={item.id} className="py-2.5">
              <p className="flex items-center gap-2 text-sm font-medium text-ink-900">
                {!item.read_at && (
                  <span className="rounded-full bg-site-alert px-1.5 py-0.5 text-[10px] font-bold text-white">
                    {text.unreadMark}
                  </span>
                )}
                {item.title}
              </p>
              <p className="mt-0.5 text-sm text-ink-700">{item.body}</p>
              <p className="mt-0.5 text-xs text-site-muted">{formatDateTime(item.sent_at)}</p>
            </li>
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
