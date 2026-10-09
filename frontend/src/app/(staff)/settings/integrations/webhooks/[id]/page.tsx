import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { buttonClass } from '@/components/ui/button';
import { EmptyState } from '@/components/ui/empty-state';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { WebhookDeliveryTable } from '@/features/integrations/components/webhook-delivery-table';
import { integrationsArea } from '@/features/integrations/content/integrations-nav';
import { deliveryLogPage } from '@/features/integrations/content/webhook-settings';
import { integrationPaths } from '@/features/integrations/lib/integration-paths';
import { loadWebhookDeliveries } from '@/features/integrations/lib/load-webhook-deliveries';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: deliveryLogPage.metaTitle };

interface WebhookLogPageProps {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ cursor?: string }>;
}

/** Every event sent to one webhook, newest first, with how its latest attempt went. */
export default async function WebhookLogPage({ params, searchParams }: WebhookLogPageProps) {
  const context = await loadStaffContext();

  if (!context?.can(Scope.IntegrationManage)) {
    return <Alert tone="info">{integrationsArea.noAccess}</Alert>;
  }

  const { id } = await params;
  const { cursor } = await searchParams;
  const page = await loadWebhookDeliveries(id, cursor);
  const next = page?.page.next_cursor;

  return (
    <div className="space-y-5">
      <BackLink href={integrationPaths.webhooks}>{deliveryLogPage.back}</BackLink>
      <PageHeader title={deliveryLogPage.title} subtitle={<span className="font-mono">{id}</span>} />
      <p className="text-sm text-ink-600">{deliveryLogPage.retryNote}</p>

      {!page ? (
        <Alert tone="info">{deliveryLogPage.notFound}</Alert>
      ) : page.data.length === 0 ? (
        <EmptyState title={deliveryLogPage.empty}>{deliveryLogPage.emptyHint}</EmptyState>
      ) : (
        <WebhookDeliveryTable rows={page.data} />
      )}

      {next && (
        <Link href={integrationPaths.webhookLogPage(id, next)} className={buttonClass('secondary', 'sm')}>
          {deliveryLogPage.next}
        </Link>
      )}
    </div>
  );
}
