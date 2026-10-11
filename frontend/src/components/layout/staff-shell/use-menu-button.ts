'use client';

import { usePathname } from 'next/navigation';
import { useCallback, useEffect, useId, useRef, useState, type KeyboardEvent } from 'react';

type FocusTarget = 'first' | 'last';

const ITEM = '[role="menuitem"]';

/**
 * A button that opens a small menu, as the WAI-ARIA menu button pattern describes it. Enter, Space
 * or the down arrow opens it on the first item and the up arrow on the last; the arrows, Home and
 * End move between items; Escape closes it and puts focus back on the button; Tab, a click outside
 * or arriving on another page closes it.
 *
 * The items are whatever the menu holds with `role="menuitem"`, links and buttons alike.
 */
export function useMenuButton() {
  const menuId = useId();
  const [open, setOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const pendingFocus = useRef<FocusTarget | null>(null);
  const pathname = usePathname();

  const items = () => Array.from(menuRef.current?.querySelectorAll<HTMLElement>(ITEM) ?? []);

  const focusAt = (index: number) => {
    const list = items();
    if (list.length > 0) list[(index + list.length) % list.length]?.focus();
  };

  const show = (target: FocusTarget) => {
    pendingFocus.current = target;
    setOpen(true);
  };

  const close = useCallback((returnFocus: boolean) => {
    setOpen(false);
    if (returnFocus) buttonRef.current?.focus();
  }, []);

  // Focus moves into the menu once it is on the page.
  useEffect(() => {
    if (!open || !pendingFocus.current) return;

    const list = Array.from(menuRef.current?.querySelectorAll<HTMLElement>(ITEM) ?? []);
    (pendingFocus.current === 'first' ? list[0] : list[list.length - 1])?.focus();
    pendingFocus.current = null;
  }, [open]);

  // A click anywhere else closes it.
  useEffect(() => {
    if (!open) return;

    const onPointerDown = (event: PointerEvent) => {
      const target = event.target as Node;
      if (!menuRef.current?.contains(target) && !buttonRef.current?.contains(target)) setOpen(false);
    };

    document.addEventListener('pointerdown', onPointerDown);
    return () => document.removeEventListener('pointerdown', onPointerDown);
  }, [open]);

  useEffect(() => setOpen(false), [pathname]);

  const buttonProps = {
    ref: buttonRef,
    type: 'button' as const,
    'aria-haspopup': 'menu' as const,
    'aria-expanded': open,
    'aria-controls': open ? menuId : undefined,
    onClick: () => (open ? close(false) : show('first')),
    onKeyDown: (event: KeyboardEvent<HTMLButtonElement>) => {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        show(event.key === 'ArrowDown' ? 'first' : 'last');
      }
    },
  };

  const menuProps = {
    ref: menuRef,
    id: menuId,
    role: 'menu' as const,
    onKeyDown: (event: KeyboardEvent<HTMLDivElement>) => {
      const list = items();
      const index = list.indexOf(document.activeElement as HTMLElement);

      switch (event.key) {
        case 'ArrowDown':
          event.preventDefault();
          focusAt(index + 1);
          break;
        case 'ArrowUp':
          event.preventDefault();
          focusAt(index - 1);
          break;
        case 'Home':
          event.preventDefault();
          focusAt(0);
          break;
        case 'End':
          event.preventDefault();
          focusAt(list.length - 1);
          break;
        case 'Escape':
          event.preventDefault();
          close(true);
          break;
        case 'Tab':
          close(false);
          break;
      }
    },
  };

  return { open, close, buttonProps, menuProps };
}
