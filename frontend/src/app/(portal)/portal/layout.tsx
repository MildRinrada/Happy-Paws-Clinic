import { redirect } from 'next/navigation';
import type { ReactNode } from 'react';
import { PortalShell } from '@/components/layout/portal-shell';
import { loadCustomerContext } from '@/features/portal/lib/customer-context';
import { loadPortalHome } from '@/features/portal/lib/load-portal-home';
import { SIGN_IN_PATH } from '@/lib/auth/session';

// Every page here is one owner's own live data. None of it is ever prerendered.
export const dynamic = 'force-dynamic';

/**
 * The customer portal, under `/portal`. The prefix is needed because route groups share one URL
 * space, and `/queue` is already the staff console's queue board.
 *
 * The middleware has sent away anyone with no session; this sends away a session that is signed in
 * but holds no customer membership. The sign-in page then forwards a member of staff to the console.
 */
export default async function PortalLayout({ children }: { children: ReactNode }) {
  const context = await loadCustomerContext();

  if (!context) {
    redirect(SIGN_IN_PATH);
  }

  const home = await loadPortalHome();

  return (
    <PortalShell displayName={home.display_name || context.me.display_name} unread={home.unread_notifications}>
      {children}
    </PortalShell>
  );
}
