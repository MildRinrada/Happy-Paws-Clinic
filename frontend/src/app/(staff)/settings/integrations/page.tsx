import { redirect } from 'next/navigation';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { integrationsNav } from '@/features/integrations/content/integrations-nav';

/** The section's own address has nothing of its own: it opens the first page this session may use. */
export default async function IntegrationsIndexPage() {
  const context = await loadStaffContext();
  const first = integrationsNav.find((item) => item.scope === null || context?.can(item.scope));

  redirect(first?.href ?? '/settings');
}
