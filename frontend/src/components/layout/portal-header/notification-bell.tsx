import Link from 'next/link';
import { portalHeader } from '@/features/portal/content/portal-navigation';
import { PortalIcon } from './portal-icon';

/** The bell, with an orange dot on it while there are unread messages. The count is in its label. */
export function NotificationBell({ unread }: { unread: number }) {
  const label = unread > 0 ? `${portalHeader.notifications.label} ${portalHeader.unread(unread)}` : portalHeader.notifications.label;

  return (
    <Link
      href={portalHeader.notifications.href}
      aria-label={label}
      className="relative flex size-10 items-center justify-center rounded-full text-ink-700 hover:bg-ink-100"
    >
      <PortalIcon name="bell" />
      {unread > 0 && (
        <span aria-hidden="true" className="absolute right-2 top-2 size-2 rounded-full bg-site-orange ring-2 ring-white" />
      )}
    </Link>
  );
}
