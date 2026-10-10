import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { Card, CardBody } from '@/components/ui/card';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { AskChat } from '@/features/staff-ai/components/ask/ask-chat';
import { UsageCard } from '@/features/staff-ai/components/ask/usage-card';
import { askPage } from '@/features/staff-ai/content/ask-page';
import { loadAiUsage } from '@/features/staff-ai/lib/load-ai-usage';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: askPage.metaTitle };

/**
 * "ผู้ช่วย AI" (docs/contract-extensions.md § 34): ask the clinic's numbers, answered from the
 * reports this session may read, and this month's allowance beside it.
 */
export default async function AiPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.AiUse)) {
    return <Alert tone="info">{askPage.noAccess}</Alert>;
  }

  const usage = await loadAiUsage();

  if (!usage) {
    return <Alert tone="info">{askPage.disabled}</Alert>;
  }

  return (
    <div className="max-w-6xl space-y-6">
      <PageHeader title={askPage.title} subtitle={askPage.subtitle} />

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <Card>
          <CardBody>
            <AskChat />
          </CardBody>
        </Card>
        <UsageCard usage={usage} />
      </div>
    </div>
  );
}
