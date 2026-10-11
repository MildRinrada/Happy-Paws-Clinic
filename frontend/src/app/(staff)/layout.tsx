import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import type { ReactNode } from 'react';
import { StaffShell } from '@/components/layout/staff-shell/staff-shell';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { loadAiUsage } from '@/features/staff-ai/lib/load-ai-usage';
import { isCollapsed, SIDEBAR_COOKIE } from '@/features/staff-shell/lib/sidebar-preference';
import { SIGN_IN_PATH } from '@/lib/auth/session';

// Every page in the console is one person's view of live data. None of it is ever prerendered.
export const dynamic = 'force-dynamic';

/**
 * The staff console. Everything under `(staff)` renders inside this.
 *
 * The middleware has already sent away anyone with no session at all; this handles the one case
 * it cannot see — a session that is signed in but is not a member of staff.
 */
export default async function StaffLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();

  if (!context) {
    redirect(SIGN_IN_PATH);
  }

  // The "ผู้ช่วย AI" menu entry needs the assistant switched on, which only the API can say.
  const aiEnabled = (await loadAiUsage()) !== null;
  const sidebarCollapsed = isCollapsed((await cookies()).get(SIDEBAR_COOKIE)?.value);

  return (
    <StaffShell context={context} aiEnabled={aiEnabled} sidebarCollapsed={sidebarCollapsed}>
      {children}
    </StaffShell>
  );
}
