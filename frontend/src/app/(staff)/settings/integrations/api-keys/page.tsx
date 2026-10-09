import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listApiKeys } from '@/features/integrations/api/apiKeys';
import { ApiKeyForm } from '@/features/integrations/components/api-key-form';
import { ApiKeyList } from '@/features/integrations/components/api-key-list';
import { apiKeySettingsPage } from '@/features/integrations/content/api-key-settings';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { apiKeyScopeOptions } from '@/features/integrations/lib/api-key-options';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: apiKeySettingsPage.metaTitle };

/** Issue a key for a machine client, shown once; see the keys there are, and revoke one. */
export default async function ApiKeysPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  const branches = Object.fromEntries(context.branches.map((branch) => [branch.id, branch.name]));
  const apiKeys = await authed(listApiKeys);

  return (
    <div className="space-y-6">
      <PageHeader title={apiKeySettingsPage.title} subtitle={apiKeySettingsPage.subtitle} />
      <ApiKeyForm scopeOptions={apiKeyScopeOptions(context.can)} branchOptions={branches} />
      <ApiKeyList apiKeys={apiKeys} branchNames={branches} />
    </div>
  );
}
