import { portalHome } from '../../content/portal-home';
import { portalPaths } from '../../content/portal-navigation';
import type { InboxItem } from '../../types/portal.types';
import { EmptyLine } from '../shared/empty-line';
import { SectionCard } from '../shared/section-card';
import { HomeNotificationRow } from './home-notification-row';

/** The three newest messages from the clinic, and the way to the whole inbox. */
export function LatestNotifications({ items }: { items: InboxItem[] }) {
  const text = portalHome.notifications;

  return (
    <SectionCard title={text.title} more={{ label: text.more, href: portalPaths.notifications }} ruled>
      {items.length === 0 ? (
        <div className="py-4">
          <EmptyLine>{text.empty}</EmptyLine>
        </div>
      ) : (
        <ul className="divide-y divide-ink-100">
          {items.map((item) => (
            <HomeNotificationRow key={item.id} item={item} />
          ))}
        </ul>
      )}
    </SectionCard>
  );
}
