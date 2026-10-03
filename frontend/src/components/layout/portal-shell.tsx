import type { ReactNode } from 'react';
import { PortalHeader } from './portal-header/portal-header';
import { PortalTabBar } from './portal-header/portal-tab-bar';
import { SiteFooter } from './site-footer/site-footer';

interface PortalShellProps {
  displayName: string;
  unread: number;
  children: ReactNode;
}

/**
 * The customer portal's chrome: the owner's own header, the public site's footer (so the clinic's
 * phone numbers and emergency line are still at hand), and on a phone the bottom menu bar.
 */
export function PortalShell({ displayName, unread, children }: PortalShellProps) {
  return (
    <div className="min-h-dvh bg-site-soft font-display text-ink-900">
      <PortalHeader displayName={displayName} unread={unread} />

      <main className="mx-auto max-w-[1040px] px-4 py-6 sm:px-6 lg:py-10">{children}</main>

      {/* The bottom bar covers the page's last 64px on a phone; this keeps the footer clear of it. */}
      <div className="pb-16 lg:pb-0">
        <SiteFooter />
      </div>

      <PortalTabBar />
    </div>
  );
}
