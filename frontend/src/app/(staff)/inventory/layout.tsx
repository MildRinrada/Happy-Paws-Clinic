import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { InventoryTabBar } from '@/features/inventory/components/inventory-tab-bar';
import { inventoryTabs } from '@/features/inventory/content/inventory-tabs';

/** Every stock page sits under one row of tabs, showing only the pages this session can use. */
export default async function InventoryLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();
  const tabs = inventoryTabs.filter((tab) => context?.can(tab.scope));

  return (
    <div className="max-w-6xl space-y-6">
      {tabs.length > 0 && <InventoryTabBar tabs={tabs} />}
      {children}
    </div>
  );
}
