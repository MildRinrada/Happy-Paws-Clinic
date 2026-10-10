import Link from 'next/link';
import { formatDateTime } from '@/lib/format/datetime';
import { versionListText } from '../../content/staff-privacy-policy';
import { formatPolicyDate } from '../../lib/policy-date';
import { policySettingsPaths } from '../../lib/privacy-paths';
import type { StaffPolicyVersion } from '../../types/staff-privacy-policy.types';
import { PolicyStatusBadge } from './policy-status-badge';

/** One version in the list: what it is, where it came from, and the way into it. */
export function PolicyVersionEntry({ version }: { version: StaffPolicyVersion }) {
  const text = versionListText;
  const title = version.label ? text.publishedTitle(version.label) : text.draftTitle;
  const origin =
    version.created_from === 'template' && version.template_version ? text.fromTemplate(version.template_version) : text.fromVersion;
  const facts = [
    origin,
    text.sections(version.sections.length),
    version.published_at ? text.publishedAt(formatDateTime(version.published_at)) : text.createdAt(formatDateTime(version.created_at)),
    version.effective_date ? text.effective(formatPolicyDate(version.effective_date)) : undefined,
    !version.published_at && version.updated_at ? text.updatedAt(formatDateTime(version.updated_at)) : undefined,
  ].filter(Boolean);

  return (
    <li className="flex flex-wrap items-center justify-between gap-3 px-5 py-3.5">
      <div className="min-w-0 space-y-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className="break-words font-medium text-ink-900">{title}</span>
          <PolicyStatusBadge version={version} />
        </div>
        <p className="break-words text-xs text-ink-600">{facts.join('  |  ')}</p>
      </div>
      <Link href={policySettingsPaths.version(version.id)} className="text-sm font-medium text-brand-700 hover:underline">
        {version.status === 'draft' ? text.edit : text.open}
      </Link>
    </li>
  );
}
