'use client';

import { useState, type ReactNode } from 'react';
import { rememberCollapsed } from '@/features/staff-shell/lib/sidebar-preference';
import type { VisibleNavGroup } from '@/features/staff-shell/types/staff-shell.types';
import { CollapseToggle } from './collapse-toggle';
import { NavMenu } from './nav-menu';

interface StaffSidebarProps {
  groups: VisibleNavGroup[];
  /** The clinic's name and logo, drawn on the server. */
  brand: ReactNode;
  /** From the cookie, so the first HTML already has the chosen width. */
  initialCollapsed: boolean;
}

/**
 * The menu down the left from 1024 px. It stays in place while the page scrolls, and scrolls on its
 * own when it is taller than the screen. Folded, it keeps only the icons: the names become tooltips
 * and stay for screen readers, and the badges shrink onto the icons' corners. Every child reads the
 * folded state from `data-collapsed` through Tailwind's named group, so none of them needs a prop.
 */
export function StaffSidebar({ groups, brand, initialCollapsed }: StaffSidebarProps) {
  const [collapsed, setCollapsed] = useState(initialCollapsed);

  const toggle = () => {
    setCollapsed(!collapsed);
    rememberCollapsed(!collapsed);
  };

  return (
    <aside
      data-collapsed={collapsed}
      className={`group/sidebar no-print sticky top-0 hidden h-dvh shrink-0 flex-col self-start border-r border-ink-200 bg-white lg:flex ${
        collapsed ? 'w-[4.5rem]' : 'w-60'
      }`}
    >
      <div className="px-4 pb-4 pt-5 group-data-[collapsed=true]/sidebar:px-0">{brand}</div>

      <div className="min-h-0 flex-1 overflow-y-auto px-3 pb-4 group-data-[collapsed=true]/sidebar:px-2">
        <NavMenu groups={groups} />
      </div>

      <div className="border-t border-ink-200 p-3 group-data-[collapsed=true]/sidebar:px-2">
        <CollapseToggle collapsed={collapsed} onToggle={toggle} />
      </div>
    </aside>
  );
}
