'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { formatDateTime } from '@/lib/format/datetime';
import { summarizePetAction } from '../../actions/staff-ai-actions';
import { aiCommon } from '../../content/ai-common';
import { petSummaryText } from '../../content/pet-summary';
import type { PetSummary } from '../../types/staff-ai.types';
import { AiDraftNotice } from '../common/ai-draft-notice';
import { CitationList } from './citation-list';
import { SummaryText } from './summary-text';

/**
 * Asks for the summary on demand, never on page load: each new summary spends a use of the
 * month's allowance. Asking again with unchanged data is served from the cache for free.
 */
export function PetSummaryPanel({ petId }: { petId: string }) {
  const text = petSummaryText;
  const [summary, setSummary] = useState<PetSummary>();
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();

  function run() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await summarizePetAction(petId);
        if (result.ok) setSummary(result.data);
        else setError(result.error.message);
      } catch {
        setError(aiCommon.unreachable);
      }
    });
  }

  return (
    <div className="space-y-3">
      {!summary && <p className="text-sm text-ink-600">{text.intro}</p>}

      {summary && (
        <div className="space-y-3 rounded-lg border border-amber-200 bg-amber-50/40 p-3">
          <AiDraftNotice />
          <SummaryText text={summary.text} citations={summary.citations} petId={petId} />
          <CitationList citations={summary.citations} petId={petId} />
          <p className="text-xs text-ink-500">
            {text.generatedAt(formatDateTime(summary.generated_at))}
            {summary.cached && ` ${text.cached}`}
          </p>
        </div>
      )}

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      <Button variant={summary ? 'secondary' : 'primary'} size="sm" onClick={run} disabled={pending}>
        {pending ? aiCommon.working : summary ? text.rerun : text.run}
      </Button>
    </div>
  );
}
