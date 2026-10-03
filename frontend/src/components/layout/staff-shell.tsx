import type { ReactNode } from 'react';
import { selectBranch } from '@/features/account/actions/select-branch';
import type { StaffContext } from '@/features/account/lib/staff-context';
import { signOut } from '@/features/authentication/actions/sign-in';
import { Scope } from '@/lib/auth/scopes';
import { BranchSwitcher } from './branch-switcher';
import { NavLink } from './nav-link';

interface NavItem {
  href: string;
  label: string;
  /** The scope the screen behind the link is built on. */
  scope: string;
}

const navigation: NavItem[] = [
  { href: '/queue', label: 'คิววันนี้', scope: Scope.QueueRead },
  { href: '/appointments', label: 'ปฏิทินนัด', scope: Scope.AppointmentRead },
  { href: '/customers', label: 'ลูกค้าและสัตว์', scope: Scope.CustomerRead },
  { href: '/billing', label: 'เก็บเงิน', scope: Scope.InvoiceWrite },
  { href: '/settings/contacts', label: 'ข้อมูลติดต่อ', scope: Scope.SettingsWrite },
];

/**
 * The staff console's chrome: who is signed in, which branch is in view, and where they can go.
 *
 * The menu lists only the screens this session's scopes can use — a cashier sees no queue, a vet
 * assistant no cash desk. That is `GET /me` used for what the contract says it is for ("บอกว่า UI
 * ต้องเปิดเมนูใดให้บัญชีนี้") and it is a convenience only: typing the address of a hidden screen
 * reaches a page whose data the backend then refuses.
 */
export function StaffShell({ context, children }: { context: StaffContext; children: ReactNode }) {
  const items = navigation.filter((item) => context.can(item.scope));

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[15rem_1fr]">
      <aside className="no-print border-b border-ink-200 bg-white px-4 py-4 lg:border-b-0 lg:border-r lg:py-6">
        <p className="px-3 font-display text-lg font-semibold text-brand-800">Happy Paws</p>
        <p className="px-3 text-xs text-ink-500">ระบบจัดการคลินิก</p>

        <nav aria-label="เมนูหลัก" className="mt-4 flex gap-1 lg:mt-6 lg:flex-col">
          {items.map((item) => (
            <NavLink key={item.href} href={item.href}>
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-col">
        <header className="no-print flex flex-wrap items-center justify-between gap-3 border-b border-ink-200 bg-white px-5 py-3">
          <BranchSwitcher
            branches={context.branches.map(({ id, name }) => ({ id, name }))}
            current={context.branch?.id}
            onSelect={selectBranch}
          />

          <div className="flex items-center gap-3">
            <p className="text-sm text-ink-700">{context.me.display_name}</p>
            <form action={signOut}>
              <button type="submit" className="rounded-lg px-2.5 py-1.5 text-sm text-ink-600 hover:bg-ink-100">
                ออกจากระบบ
              </button>
            </form>
          </div>
        </header>

        <main className="min-w-0 flex-1 px-5 py-6">{children}</main>
      </div>
    </div>
  );
}
