import { aiCommon } from '../../content/ai-common';
import { AiDraftBadge } from './ai-draft-badge';

/** The badge and the one line that says to check before use, above any AI text. */
export function AiDraftNotice() {
  return (
    <p className="flex flex-wrap items-center gap-2 text-xs text-amber-900">
      <AiDraftBadge />
      <span>{aiCommon.draftNotice}</span>
    </p>
  );
}
