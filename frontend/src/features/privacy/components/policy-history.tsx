import Link from 'next/link';
import { historyText } from '../content/privacy-policy';
import { formatPolicyDate } from '../lib/policy-date';
import { privacyVersionHref } from '../lib/privacy-paths';
import type { PolicyHistoryEntry } from '../types/privacy-policy.types';
import { PolicySection } from './policy-section';

interface PolicyHistoryProps {
  entries: PolicyHistoryEntry[];
  /** The version on screen, which is not a link. */
  shown?: string;
}

/** Every published version, newest first, each readable on its own page. Drawn when there is more than one. */
export function PolicyHistory({ entries, shown }: PolicyHistoryProps) {
  const [current] = entries;

  return (
    <PolicySection id="versions" heading={historyText.heading}>
      <p className="text-[15px] leading-relaxed text-ink-700">{historyText.lead}</p>
      <ul className="space-y-2 text-[15px]">
        {entries.map((entry) => {
          const label = historyText.entry(entry.label, formatPolicyDate(entry.effective_date));
          const isCurrent = entry.label === current?.label;

          return (
            <li key={entry.label} className="break-words">
              {entry.label === shown ? (
                <span className="font-semibold text-ink-900">{label}</span>
              ) : (
                <Link href={isCurrent ? '/privacy' : privacyVersionHref(entry.label)} className="text-site-teal hover:underline">
                  {label}
                </Link>
              )}
              {isCurrent && <span className="ml-2 text-sm text-site-muted">({historyText.current})</span>}
            </li>
          );
        })}
      </ul>
    </PolicySection>
  );
}
