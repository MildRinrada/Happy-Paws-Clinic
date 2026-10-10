import Link from 'next/link';
import { formatLongDate } from '@/lib/format/datetime';
import { citationKinds } from '../../content/citation-kinds';
import { citationHref } from '../../lib/citation-href';
import type { PetSummaryCitation } from '../../types/staff-ai.types';

/** One source of the summary: what it is and when, linked to where it can be read in full. */
export function CitationChip({ citation, petId }: { citation: PetSummaryCitation; petId: string }) {
  const href = citationHref(citation, petId);
  const label = `${citation.ref} ${citationKinds[citation.kind] ?? citation.kind} ${formatLongDate(citation.date)}`;
  const style = 'inline-flex items-center rounded-full border px-2 py-0.5 text-xs';

  return href ? (
    <Link href={href} className={`${style} border-brand-200 bg-brand-50 text-brand-800 hover:bg-brand-100`}>
      {label}
    </Link>
  ) : (
    <span className={`${style} border-ink-200 bg-ink-50 text-ink-700`}>{label}</span>
  );
}
