import type { Metadata } from 'next';
import { Alert } from '@/components/ui/alert';
import { BackLink } from '@/components/ui/back-link';
import { PageHeader } from '@/components/ui/page-header';
import { loadStaffContext } from '@/features/account/lib/staff-context';
import { listPolicyVersions } from '@/features/privacy/api/managePrivacyPolicy';
import { DraftEditor } from '@/features/privacy/components/staff/draft-editor';
import { DraftPreview } from '@/features/privacy/components/staff/draft-preview';
import { PolicyStatusBadge } from '@/features/privacy/components/staff/policy-status-badge';
import { PublishedVersionView } from '@/features/privacy/components/staff/published-version-view';
import { TemplateDraftWarning } from '@/features/privacy/components/staff/template-draft-warning';
import { editorText, staffPolicyText, versionListText } from '@/features/privacy/content/staff-privacy-policy';
import { policySettingsPaths } from '@/features/privacy/lib/privacy-paths';
import { Scope } from '@/lib/auth/scopes';
import { authed } from '@/lib/auth/session';

export const metadata: Metadata = { title: staffPolicyText.metaTitle };

/**
 * One version: a draft opens in the editor (with the publish form for the owner), a published one
 * opens read only. There is no single-version read in the API, so the list is read and the version
 * picked out of it; a clinic keeps a handful of versions.
 */
export default async function PrivacyPolicyVersionPage({ params }: { params: Promise<{ id: string }> }) {
  const [context, { id }] = await Promise.all([loadStaffContext(), params]);

  if (!context?.can(Scope.SettingsRead)) {
    return <Alert tone="info">{staffPolicyText.noAccess}</Alert>;
  }

  const list = await authed(listPolicyVersions);
  const version = list.data.find((entry) => entry.id === id);
  const canDraft = context.can(Scope.SettingsWrite);

  if (!version) {
    return (
      <div className="space-y-6">
        <BackLink href={policySettingsPaths.list}>{staffPolicyText.back}</BackLink>
        <Alert tone="info">{staffPolicyText.missing}</Alert>
      </div>
    );
  }

  const title = version.label ? versionListText.publishedTitle(version.label) : editorText.heading;

  return (
    <div className="space-y-6">
      <BackLink href={policySettingsPaths.list}>{staffPolicyText.back}</BackLink>
      <PageHeader title={title} actions={<PolicyStatusBadge version={version} />} />

      {version.status === 'draft' && version.created_from === 'template' && <TemplateDraftWarning />}

      {version.status === 'published' ? (
        <PublishedVersionView version={version} canDraft={canDraft} />
      ) : canDraft ? (
        <DraftEditor key={version.id} draft={version} canPublish={context.can(Scope.SettingsDanger)} />
      ) : (
        <DraftPreview sections={version.sections} />
      )}
    </div>
  );
}
