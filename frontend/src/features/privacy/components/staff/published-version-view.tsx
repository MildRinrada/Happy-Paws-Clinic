import Link from 'next/link';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { formatDateTime } from '@/lib/format/datetime';
import { createDraftText, publishedViewText } from '../../content/staff-privacy-policy';
import { formatPolicyDate } from '../../lib/policy-date';
import { privacyVersionHref } from '../../lib/privacy-paths';
import type { StaffPolicyVersion } from '../../types/staff-privacy-policy.types';
import { CreateDraftButton } from './create-draft-button';
import { DraftPreview } from './draft-preview';

interface PublishedVersionViewProps {
  version: StaffPolicyVersion;
  /** True for `settings:write` holders, who may copy it into a new draft. */
  canDraft: boolean;
}

/** A published version, read only: its facts, its text as the public page draws it, and "copy to a new draft". */
export function PublishedVersionView({ version, canDraft }: PublishedVersionViewProps) {
  const text = publishedViewText;
  const rows = [
    { term: text.label, detail: version.label },
    { term: text.effective, detail: version.effective_date ? formatPolicyDate(version.effective_date) : undefined },
    { term: text.publishedAt, detail: version.published_at ? formatDateTime(version.published_at) : undefined },
    { term: text.legal, detail: version.legal_review_done ? text.legalYes : text.legalNo },
  ];

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader title={text.heading} />
        <CardBody className="space-y-4">
          <dl className="grid gap-3 text-sm sm:grid-cols-2">
            {rows.map((row) => (
              <div key={row.term} className="min-w-0">
                <dt className="text-ink-500">{row.term}</dt>
                <dd className="break-words font-medium text-ink-900">{row.detail ?? '-'}</dd>
              </div>
            ))}
          </dl>
          <p className="text-sm text-ink-600">{text.fixed}</p>
          <div className="flex flex-wrap items-center gap-3">
            {version.label && (
              <Link href={privacyVersionHref(version.label)} target="_blank" className="text-sm font-medium text-brand-700 hover:underline">
                {text.viewPublic}
              </Link>
            )}
            {canDraft && (
              <CreateDraftButton source={{ source: 'version', version_id: version.id }} label={createDraftText.fromThis} />
            )}
          </div>
        </CardBody>
      </Card>

      <DraftPreview sections={version.sections} />
    </div>
  );
}
