import Link from 'next/link';
import { citationHref } from '../../lib/citation-href';
import { splitCitations } from '../../lib/split-citations';
import type { PetSummaryCitation } from '../../types/staff-ai.types';

/**
 * The paragraph, with each `[R3]` drawn as a small marker that opens its record when it has a page
 * of its own. The API has already removed any marker the model made up.
 */
export function SummaryText({ text, citations, petId }: { text: string; citations: PetSummaryCitation[]; petId: string }) {
  const byRef = new Map(citations.map((citation) => [citation.ref, citation]));

  return (
    <p className="whitespace-pre-line text-sm leading-relaxed text-ink-900">
      {splitCitations(text).map((part, index) => {
        if (part.kind === 'text') return <span key={index}>{part.text}</span>;

        const citation = byRef.get(part.ref);
        const href = citation ? citationHref(citation, petId) : undefined;
        const marker = <sup className="mx-0.5 rounded bg-brand-50 px-1 text-[10px] font-semibold text-brand-800">{part.ref}</sup>;

        return href ? (
          <Link key={index} href={href} className="hover:underline">
            {marker}
          </Link>
        ) : (
          <span key={index}>{marker}</span>
        );
      })}
    </p>
  );
}
