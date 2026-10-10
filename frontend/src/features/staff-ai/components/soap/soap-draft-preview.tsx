import type { SoapDraft } from '../../types/staff-ai.types';
import { AiDraftNotice } from '../common/ai-draft-notice';
import { DraftField } from './draft-field';
import { DraftFindings } from './draft-findings';
import { DraftWarnings } from './draft-warnings';

const FIELDS = ['chief_complaint', 'subjective', 'objective', 'assessment', 'plan'] as const;

/** The draft as it would go into the form, with what to check first above it. */
export function SoapDraftPreview({ draft }: { draft: SoapDraft }) {
  return (
    <div className="space-y-3 rounded-lg border border-amber-200 bg-amber-50/40 p-3">
      <AiDraftNotice />
      <DraftWarnings warnings={draft.warnings} />
      <dl className="space-y-2">
        {FIELDS.map((field) => (
          <DraftField key={field} name={field} value={draft.draft[field]} />
        ))}
      </dl>
      <DraftFindings findings={draft.draft.examination} />
    </div>
  );
}
