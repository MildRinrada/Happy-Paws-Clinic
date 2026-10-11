import type { ReactNode } from 'react';
import type { StaffContext } from '@/features/account/lib/staff-context';
import { createActions } from '@/features/staff-shell/lib/create-actions';
import { loadNavCounts } from '@/features/staff-shell/lib/load-nav-counts';
import { visibleNavigation } from '@/features/staff-shell/lib/visible-navigation';
import type { NavCounts } from '@/features/staff-shell/types/staff-shell.types';
import { ClinicBrand } from './clinic-brand';
import { NavCountsProvider } from './nav-counts-provider';
import { StaffSidebar } from './staff-sidebar';
import { TopBar } from './top-bar';

interface StaffShellProps {
  context: StaffContext;
  /** True when this session holds `ai:use` and the assistant is on (docs/contract-extensions.md § 34). */
  aiEnabled?: boolean;
  /** The sidebar folded to icons, as this browser last left it. */
  sidebarCollapsed?: boolean;
  children: ReactNode;
}

/**
 * The staff console's chrome: the grouped menu down the left, and the top bar with search, the
 * branch in view, the clinic's time, "+ สร้าง" and the account menu.
 *
 * Nothing here holds the page back. The menu's counts are a promise handed to the browser
 * unawaited, and the clinic's name and hours stream in behind their own Suspense boundaries, so
 * the page under the frame renders as soon as its own data is in.
 */
export function StaffShell({ context, aiEnabled = false, sidebarCollapsed = false, children }: StaffShellProps) {
  const groups = visibleNavigation(context, aiEnabled);
  // A failure is already per badge inside; this only keeps a stray throw from reaching the browser.
  const counts: Promise<NavCounts> = loadNavCounts(context).catch(() => ({}));

  return (
    <NavCountsProvider initial={counts}>
      <div className="min-h-dvh lg:flex">
        <StaffSidebar groups={groups} brand={<ClinicBrand />} initialCollapsed={sidebarCollapsed} />

        <div className="flex min-w-0 flex-1 flex-col">
          <TopBar context={context} groups={groups} createActions={createActions(context.can)} now={new Date().toISOString()} />
          <main className="min-w-0 flex-1 px-5 py-6">{children}</main>
        </div>
      </div>
    </NavCountsProvider>
  );
}
