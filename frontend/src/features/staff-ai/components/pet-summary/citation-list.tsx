import { petSummaryText } from '../../content/pet-summary';
import type { PetSummaryCitation } from '../../types/staff-ai.types';
import { CitationChip } from './citation-chip';

/** Every record the summary cites, in the order it first cites them. */
export function CitationList({ citations, petId }: { citations: PetSummaryCitation[]; petId: string }) {
  if (citations.length === 0) {
    return <p className="text-xs text-ink-500">{petSummaryText.noSources}</p>;
  }

  return (
    <div className="space-y-1.5">
      <p className="text-xs font-medium text-ink-600">{petSummaryText.sources}</p>
      <ul className="flex flex-wrap gap-1.5">
        {citations.map((citation) => (
          <li key={citation.ref}>
            <CitationChip citation={citation} petId={petId} />
          </li>
        ))}
      </ul>
    </div>
  );
}
