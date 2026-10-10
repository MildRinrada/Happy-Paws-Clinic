import type { Metadata } from 'next';
import Link from 'next/link';
import { Alert } from '@/components/ui/alert';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listPolicyVersions } from '@/features/privacy/api/managePrivacyPolicy';
import { CreateDraftButton } from '@/features/privacy/components/staff/create-draft-button';
import { NewerTemplateNotice } from '@/features/privacy/components/staff/newer-template-notice';
import { NotPublishedWarning } from '@/features/privacy/components/staff/not-published-warning';
import { PolicyVersionList } from '@/features/privacy/components/staff/policy-version-list';
import { createDraftText, staffPolicyText } from '@/features/privacy/content/staff-privacy-policy';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: staffPolicyText.metaTitle };

/**
 * The clinic's privacy notice in the console (docs/contract-extensions.md § 35): every version,
 * whether one is published, whether a newer template shipped, and the ways to start a draft.
 * Reading is `settings:read`, drafting `settings:write`, publishing `settings:danger`.
 */
export default async function PrivacyPolicySettingsPage() {
  const context = await loadStaffContext();

  if (!context?.can(Scope.SettingsRead)) {
    return <Alert tone="info">{staffPolicyText.noAccess}</Alert>;
  }

  const list = await authed(listPolicyVersions);
  const canDraft = context.can(Scope.SettingsWrite);
  const published = Boolean(list.current_version_id);

  return (
    <div className="space-y-6">
      <PageHeader
        title={staffPolicyText.title}
        subtitle={staffPolicyText.subtitle}
        actions={
          <Link href="/privacy" target="_blank" className="text-sm font-medium text-brand-700 hover:underline">
            {staffPolicyText.viewPublic}
          </Link>
        }
      />

      {!published && <NotPublishedWarning canPublish={context.can(Scope.SettingsDanger)} />}
      {list.template.newer_available && <NewerTemplateNotice version={list.template.version} />}

      {canDraft && (
        <div className="flex flex-wrap items-start gap-3">
          {published && <CreateDraftButton source={{ source: 'current' }} label={createDraftText.fromCurrent} variant="primary" />}
          <CreateDraftButton source={{ source: 'template' }} label={createDraftText.fromTemplate} />
        </div>
      )}

      <PolicyVersionList versions={list.data} />
    </div>
  );
}
