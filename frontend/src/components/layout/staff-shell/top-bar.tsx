import { selectBranch } from '@/features/account/actions/select-branch';
import type { StaffContext } from '@/features/account/lib/staff-context';
import { initialsOf, staffRoleOf } from '@/features/staff-shell/lib/staff-identity';
import type { CreateAction, VisibleNavGroup } from '@/features/staff-shell/types/staff-shell.types';
import { Scope } from '@/lib/auth/scopes';
import { AccountMenu } from './account-menu';
import { BranchSwitcher } from './branch-switcher';
import { ClinicBrand } from './clinic-brand';
import { CreateMenu } from './create-menu';
import { GlobalSearch } from './global-search';
import { MobileNav } from './mobile-nav';
import { TopBarClock } from './top-bar-clock';

interface TopBarProps {
  context: StaffContext;
  groups: VisibleNavGroup[];
  createActions: CreateAction[];
  /** The instant the page was drawn, for the clock's first render. */
  now: string;
}

/**
 * The bar across the top of every console page: search, the branch in view with the clinic's time,
 * "+ สร้าง", and the account menu. Below 1024 px it also carries the ☰ button and the clinic's name,
 * and the branch and time move to a second row; from 1280 px everything sits on one line.
 */
export function TopBar({ context, groups, createActions, now }: TopBarProps) {
  const name = context.me.display_name;

  return (
    <header className="no-print relative flex flex-wrap items-center gap-x-2 gap-y-2 border-b border-ink-200 bg-white px-4 py-2.5 sm:gap-x-3 lg:px-5">
      <MobileNav groups={groups} brand={<ClinicBrand compact />} />
      <div className="min-w-0 flex-1 lg:hidden">
        <ClinicBrand compact />
      </div>

      {context.can(Scope.CustomerRead) && <GlobalSearch withPets={context.can(Scope.PatientRead)} />}

      <div className="order-last flex w-full min-w-0 items-center justify-between gap-3 xl:order-none xl:ml-auto xl:w-auto xl:justify-end xl:gap-4">
        <BranchSwitcher
          branches={context.branches.map(({ id, name: branchName }) => ({ id, name: branchName }))}
          current={context.branch?.id}
          onSelect={selectBranch}
        />
        <TopBarClock now={now} branchId={context.branch?.id} />
      </div>

      <div className="ml-auto flex items-center gap-2 xl:ml-0">
        <CreateMenu actions={createActions} />
        <AccountMenu name={name} initials={initialsOf(name)} role={staffRoleOf(context.me)} />
      </div>
    </header>
  );
}
