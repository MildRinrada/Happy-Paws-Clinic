import { redirect } from 'next/navigation';
import { Alert } from '@/components/ui/alert';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { crmArea, crmTabs } from '@/features/crm/content/crm-tabs';

/** `/crm` itself has nothing of its own: it opens the first page this session can use. */
export default async function CrmIndexPage() {
  const context = await loadStaffContext();
  const first = crmTabs.find((tab) => context?.can(tab.scope));

  if (first) {
    redirect(first.href);
  }

  return <Alert tone="info">{crmArea.noAccess}</Alert>;
}
