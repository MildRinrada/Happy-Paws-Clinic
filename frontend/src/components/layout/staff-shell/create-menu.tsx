'use client';

import Link from 'next/link';
import { buttonClass } from '@/components/ui/button';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { CreateAction } from '@/features/staff-shell/types/staff-shell.types';
import { MenuPanel } from './menu-panel';
import { menuItemClass } from './menu-item-class';
import { StaffIcon } from './staff-icon';
import { useMenuButton } from './use-menu-button';

/**
 * "+ สร้าง": the counter's everyday jobs from any page, each to the screen that already does it.
 * The list is the overview's shortcuts, already filtered by scope on the server; with none left the
 * button is not drawn. On a phone the button is the plus sign alone.
 */
export function CreateMenu({ actions }: { actions: CreateAction[] }) {
  const { open, close, buttonProps, menuProps } = useMenuButton();
  const text = staffShellText.create;

  if (actions.length === 0) {
    return null;
  }

  return (
    <div className="relative">
      <button {...buttonProps} aria-label={text.menu} className={buttonClass('primary', 'md', 'px-2.5 sm:px-3.5')}>
        <StaffIcon name="plus" className="size-5" />
        <span className="hidden sm:inline">{text.button}</span>
        <StaffIcon name="chevronDown" className="hidden size-4 sm:block" />
      </button>

      {open && (
        <MenuPanel {...menuProps} aria-label={text.menu} className="w-56">
          {actions.map((action) => (
            <Link key={action.key} href={action.href} role="menuitem" className={menuItemClass} onClick={() => close(false)}>
              {action.label}
            </Link>
          ))}
        </MenuPanel>
      )}
    </div>
  );
}
