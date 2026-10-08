import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listTemplates } from '@/features/notifications/api/listTemplates';
import { TemplateCard } from '@/features/notifications/components/template-card';
import { templatesPage } from '@/features/notifications/content/templates-page';
import { authed } from '@/lib/auth/session';
import { Scope } from '@/lib/auth/scopes';

export const metadata: Metadata = { title: templatesPage.metaTitle };

/** What every message says, per channel, read-only because the contract gives templates a read only. */
export default async function TemplatesPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.NotifyManage)) {
    return <Alert tone="info">{templatesPage.noAccess}</Alert>;
  }

  const templates = await authed(listTemplates);

  return (
    <div className="space-y-5">
      <PageHeader title={templatesPage.title} subtitle={templatesPage.subtitle} />
      <Alert tone="info">{templatesPage.readOnly}</Alert>
      <div className="space-y-4">
        {templates.map((template) => (
          <TemplateCard key={template.key} template={template} />
        ))}
      </div>
    </div>
  );
}
