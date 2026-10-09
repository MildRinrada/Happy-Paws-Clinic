import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listWebhooks } from '@/features/integrations/api/webhooks';
import { WebhookForm } from '@/features/integrations/components/webhook-form';
import { WebhookList } from '@/features/integrations/components/webhook-list';
import { WebhookLogLookup } from '@/features/integrations/components/webhook-log-lookup';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { webhookSettingsPage } from '@/features/integrations/content/webhook-settings';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: webhookSettingsPage.metaTitle };

/** Make an outbound webhook, manage the ones there are, and open a deleted one's log by id. */
export default async function WebhooksPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  const webhooks = await authed(listWebhooks);

  return (
    <div className="space-y-6">
      <PageHeader title={webhookSettingsPage.title} subtitle={webhookSettingsPage.subtitle} />
      <WebhookForm />
      <WebhookList webhooks={webhooks} />
      <WebhookLogLookup />
    </div>
  );
}
