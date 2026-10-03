import Link from 'next/link';
import { portalHeader } from '@/features/portal/content/portal-navigation';
import { PortalIcon } from './portal-icon';

/** The bell, with the number of unread messages on it when there are any. */
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
        <span className="absolute right-1 top-1 flex h-[18px] min-w-[18px] items-center justify-center rounded-full bg-site-alert px-1 text-[11px] font-bold leading-none text-white">
          {unread > 99 ? '99+' : unread}
        </span>
      )}
    </Link>
  );
}
