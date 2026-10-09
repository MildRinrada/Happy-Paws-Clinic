import Link from 'next/link';
import type { ReactNode } from 'react';
import { selectBranch } from '@/features/account/actions/select-branch';
import type { StaffContext } from '@/features/account/lib/staff-context';
import { signOut } from '@/features/authentication/actions/sign-in';
import { settingsTabs } from '@/features/settings/content/settings-tabs';
import { Scope } from '@/lib/auth/scopes';
import { BranchSwitcher } from './branch-switcher';
import { NavLink } from './nav-link';

interface NavItem {
  href: string;
  label: string;
  /** The scopes the screens behind the link are built on; holding any one shows the link. */
  scopes: string[];
}

const navigation: NavItem[] = [
  { href: '/dashboard', label: 'ภาพรวม', scopes: [Scope.DashboardRead, Scope.StatsRead, Scope.AnalyticsRead] },
  { href: '/queue', label: 'คิววันนี้', scopes: [Scope.QueueRead] },
  { href: '/appointments', label: 'ปฏิทินนัด', scopes: [Scope.AppointmentRead] },
  { href: '/customers', label: 'ลูกค้าและสัตว์', scopes: [Scope.CustomerRead] },
  { href: '/lab', label: 'แล็บ', scopes: [Scope.LabRead, Scope.LabWrite] },
  { href: '/prescriptions', label: 'จ่ายยา', scopes: [Scope.PrescriptionDispense] },
  { href: '/inventory', label: 'สต๊อกยา', scopes: [Scope.InventoryRead] },
  { href: '/billing', label: 'เก็บเงิน', scopes: [Scope.InvoiceWrite] },
  { href: '/messages', label: 'ข้อความลูกค้า', scopes: [Scope.MessageRead] },
  { href: '/crm', label: 'แพ็กเกจและโปรโมชัน', scopes: [Scope.CrmRead, Scope.CrmManage] },
  { href: '/notifications', label: 'แจ้งเตือนลูกค้า', scopes: [Scope.NotifyRead, Scope.NotifySend, Scope.AutomationRead] },
  { href: '/schedules', label: 'ตารางเวร', scopes: [Scope.ScheduleRead] },
  { href: '/settings', label: 'ตั้งค่า', scopes: [Scope.SettingsWrite, Scope.ServiceManage, Scope.StaffRead] },
];

/**
 * Where a menu entry goes. Settings opens straight on the first tab this session can use, rather
 * than on `/settings`, which would only redirect there: one round trip instead of two.
 */
function entryOf(href: string, context: StaffContext): string {
  return href === '/settings' ? (settingsTabs.find((tab) => context.can(tab.scope))?.href ?? href) : href;
}

/**
 * The staff console's chrome: who is signed in, which branch is in view, and where they can go.
 *
 * The menu lists only the screens this session's scopes can use — a cashier sees no queue, a vet
 * assistant no cash desk. That is `GET /me` used for what the contract says it is for ("บอกว่า UI
 * ต้องเปิดเมนูใดให้บัญชีนี้") and it is a convenience only: typing the address of a hidden screen
 * reaches a page whose data the backend then refuses.
 */
export function StaffShell({ context, children }: { context: StaffContext; children: ReactNode }) {
  const items = navigation.filter((item) => item.scopes.some(context.can));

  return (
    <div className="min-h-dvh lg:grid lg:grid-cols-[15rem_1fr]">
      <aside className="no-print border-b border-ink-200 bg-white px-4 py-4 lg:border-b-0 lg:border-r lg:py-6">
        <p className="px-3 font-display text-lg font-semibold text-brand-800">Happy Paws</p>
        <p className="px-3 text-xs text-ink-500">ระบบจัดการคลินิก</p>

        <nav aria-label="เมนูหลัก" className="-mx-4 mt-4 flex gap-1 overflow-x-auto px-4 lg:mx-0 lg:mt-6 lg:flex-col lg:overflow-visible lg:px-0">
          {items.map((item) => (
            <NavLink key={item.href} href={entryOf(item.href, context)} section={item.href}>
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
            <Link href="/account" className="rounded-lg px-2 py-1.5 text-sm text-ink-700 hover:bg-ink-100" title="บัญชีของฉัน">
              {context.me.display_name}
            </Link>
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
