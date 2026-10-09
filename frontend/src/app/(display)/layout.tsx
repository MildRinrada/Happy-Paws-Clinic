import { redirect } from 'next/navigation';
import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { SIGN_IN_PATH } from '@/lib/auth/session';

// A live screen of today's queue. Never prerendered.
export const dynamic = 'force-dynamic';

/**
 * Full-screen pages for a screen nobody sits at: the waiting-room TV at `/queue/display`.
 *
 * Its own route group so the page escapes the staff shell (no sidebar, no top bar) while its URL
 * stays under `/queue`, which the middleware already matches and keeps signed in. The session is
 * still a member of staff's, checked here the way the console's layout checks it.
 */
export default async function DisplayLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();

  if (!context) {
    redirect(SIGN_IN_PATH);
  }

  return <div className="min-h-dvh bg-ink-900 text-white">{children}</div>;
}
