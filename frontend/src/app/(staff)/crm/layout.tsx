import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { CrmTabBar } from '@/features/crm/components/crm-tab-bar';
import { crmTabs } from '@/features/crm/content/crm-tabs';

/** Every CRM page sits under one row of tabs, showing only the pages this session can use. */
export default async function CrmLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();
  const tabs = crmTabs.filter((tab) => context?.can(tab.scope));

  return (
    <div className="max-w-5xl space-y-6">
      {tabs.length > 0 && <CrmTabBar tabs={tabs} />}
      {children}
    </div>
  );
}
