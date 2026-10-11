'use client';

import Link from 'next/link';
import { signOut } from '@/features/authentication/actions/sign-in';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import { AccountAvatar } from './account-avatar';
import { MenuPanel } from './menu-panel';
import { menuItemClass } from './menu-item-class';
import { StaffIcon } from './staff-icon';
import { useMenuButton } from './use-menu-button';

interface AccountMenuProps {
  name: string;
  initials: string;
  /** The role the clinic gave this account, in Thai. Absent when the account has none to show. */
  role?: string;
}

/**
 * Who is signed in, as a button with their initials (and from 1280 px their name), opening their
 * own page and sign out. Signing out is the same form posting to the server action as before.
 */
export function AccountMenu({ name, initials, role }: AccountMenuProps) {
  const { open, close, buttonProps, menuProps } = useMenuButton();
  const text = staffShellText.account;

  return (
    <div className="relative">
      <button
        {...buttonProps}
        aria-label={`${text.menu} ${name}`}
        className="flex items-center gap-2 rounded-full p-1 hover:bg-ink-100 xl:pr-2"
      >
        <AccountAvatar initials={initials} />
        <span className="hidden max-w-[10rem] truncate text-sm font-medium text-ink-800 xl:block">{name}</span>
        <StaffIcon name="chevronDown" className="hidden size-4 text-ink-500 xl:block" />
      </button>

      {open && (
        <MenuPanel {...menuProps} aria-label={text.menu} className="w-64">
          <div className="flex items-center gap-3 px-3 pb-3 pt-2">
            <AccountAvatar initials={initials} className="size-10 text-base" />
            <div className="min-w-0">
              <p className="truncate text-sm font-semibold text-ink-900">{name}</p>
              {role && <p className="truncate text-xs text-ink-500">{role}</p>}
            </div>
          </div>
          <hr className="mx-2 mb-1 border-ink-100" />
          <Link href="/account" role="menuitem" className={menuItemClass} onClick={() => close(false)}>
            <StaffIcon name="account" className="size-4 text-ink-500" />
            {text.myAccount}
          </Link>
          <form action={signOut}>
            <button type="submit" role="menuitem" className={menuItemClass}>
              <StaffIcon name="signOut" className="size-4 text-ink-500" />
              {text.signOut}
            </button>
          </form>
        </MenuPanel>
      )}
    </div>
  );
}
