import type { ReactNode } from 'react';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { IntegrationsNav } from '@/features/integrations/components/integrations-nav';
import { integrationsNav } from '@/features/integrations/content/integrations-nav';

/** The "เชื่อมต่อระบบ" pages share one row of links, showing the ones this session may open. */
export default async function IntegrationsLayout({ children }: { children: ReactNode }) {
  const context = await loadStaffContext();
  const items = integrationsNav.filter((item) => item.scope === null || context?.can(item.scope));

  return (
    <div className="space-y-6">
      {items.length > 1 && <IntegrationsNav items={items} />}
      {children}
    </div>
  );
}
