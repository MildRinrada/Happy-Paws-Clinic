'use client';

import { usePathname } from 'next/navigation';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import { staffShellText } from '@/features/staff-shell/content/staff-shell-text';
import type { VisibleNavGroup } from '@/features/staff-shell/types/staff-shell.types';
import { NavMenu } from './nav-menu';
import { StaffIcon } from './staff-icon';

interface MobileNavProps {
  groups: VisibleNavGroup[];
  brand: ReactNode;
}

/**
 * The ☰ button below 1024 px, opening the same grouped menu in a drawer from the left.
 *
 * The drawer is a modal `<dialog>`: the browser makes the page behind it inert, so focus stays in
 * the drawer, and Escape closes it. The page behind does not scroll while it is open. It closes on
 * its own when a link is followed or the page changes some other way.
 */
export function MobileNav({ groups, brand }: MobileNavProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const [open, setOpen] = useState(false);
  const pathname = usePathname();

  useEffect(() => setOpen(false), [pathname]);

  useEffect(() => {
    const element = dialog.current;
    if (!element) return;

    if (!open) {
      if (element.open) element.close();
      return;
    }

    element.showModal();
    const previous = document.documentElement.style.overflow;
    document.documentElement.style.overflow = 'hidden';

    return () => {
      document.documentElement.style.overflow = previous;
    };
  }, [open]);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        aria-label={staffShellText.openMenu}
        aria-expanded={open}
        className="-ml-1 flex size-10 items-center justify-center rounded-lg text-ink-700 hover:bg-ink-100 lg:hidden"
      >
        <StaffIcon name="menu" className="size-6" />
      </button>

      <dialog
        ref={dialog}
        aria-label={staffShellText.mainMenu}
        onClose={() => setOpen(false)}
        // A click on the backdrop lands on the dialog itself, outside the panel inside it.
        onClick={(event) => event.target === event.currentTarget && setOpen(false)}
        className="no-print m-0 h-dvh max-h-none w-72 max-w-[85vw] bg-white p-0 shadow-xl backdrop:bg-ink-900/40 lg:hidden"
      >
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between gap-2 border-b border-ink-200 px-4 py-3">
            {brand}
            <button
              type="button"
              onClick={() => setOpen(false)}
              aria-label={staffShellText.closeMenu}
              className="flex size-10 shrink-0 items-center justify-center rounded-lg text-ink-600 hover:bg-ink-100"
            >
              <StaffIcon name="close" className="size-5" />
            </button>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto px-3 py-4">
            <NavMenu groups={groups} onNavigate={() => setOpen(false)} />
          </div>
        </div>
      </dialog>
    </>
  );
}
