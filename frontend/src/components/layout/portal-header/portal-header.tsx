import { CtaLink } from '@/components/ui/cta-link';
import { portalHeader } from '@/features/portal/content/portal-navigation';
import { AccountMenu } from './account-menu';
import { NotificationBell } from './notification-bell';
import { PortalLogo } from './portal-logo';
import { PortalNav } from './portal-nav';

interface PortalHeaderProps {
  displayName: string;
  unread: number;
}

/**
 * The header a signed-in owner sees in the portal: the clinic's logo (back to its site), the
 * owner's own menu, the bell, booking, and their account. On a phone it is the logo, the bell and
 * the account only; booking is the first shortcut on the home page and the menu is the bottom bar.
 */
export function PortalHeader({ displayName, unread }: PortalHeaderProps) {
  return (
    <header className="sticky top-0 z-20 border-b border-ink-200 bg-white">
      <div className="mx-auto flex h-[68px] max-w-[1120px] items-center gap-3 px-4 sm:px-6 xl:h-[72px] xl:gap-12">
        <PortalLogo />
        <PortalNav />

        <div className="ml-auto flex items-center gap-2 sm:gap-3">
          <NotificationBell unread={unread} />
          {/* A wrapper, because the link's own `inline-flex` would outweigh a `hidden` put on it. */}
          <div className="hidden sm:block">
            <CtaLink href={portalHeader.book.href}>{portalHeader.book.label}</CtaLink>
          </div>
          <AccountMenu displayName={displayName} />
        </div>
      </div>
    </header>
  );
}
