'use client';

import { useEffect, useRef, useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { askAction } from '../../actions/staff-ai-actions';
import { aiCommon } from '../../content/ai-common';
import { askPage } from '../../content/ask-page';
import type { AskEntry } from '../../types/staff-ai.types';
import { AskBubble } from './ask-bubble';
import { AskInput } from './ask-input';
import { AskSuggestions } from './ask-suggestions';

/** The API takes at most 20 messages; older ones fall off the front and the conversation goes on. */
const HISTORY = 20;

/**
 * Ask the clinic's numbers. The whole conversation is sent with each question and kept only in
 * this page: nothing is stored on the server, and leaving the page ends it.
 */
export function AskChat() {
  const [entries, setEntries] = useState<AskEntry[]>([]);
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();
  const end = useRef<HTMLLIElement>(null);

  useEffect(() => {
    end.current?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
  }, [entries, pending]);

  function send(question: string) {
    const message = question.trim();
    if (!message || pending) return;

    const next: AskEntry[] = [...entries, { role: 'user', text: message }];
    setEntries(next);
    setError(undefined);

    startTransition(async () => {
      try {
        const history = next.slice(-HISTORY).map(({ role, text }) => ({ role, text }));
        const result = await askAction(history);

        if (result.ok) setEntries([...next, { role: 'assistant', text: result.data.reply, actions: result.data.actions }]);
        else setError(result.error.message);
      } catch {
        setError(aiCommon.unreachable);
      }
    });
  }

  return (
    <div className="space-y-4">
      <ol className="max-h-[60vh] space-y-3 overflow-y-auto" aria-live="polite">
        <AskBubble entry={{ role: 'assistant', text: askPage.greeting }} />
        {entries.map((entry, index) => (
          <AskBubble key={index} entry={entry} />
        ))}
        {pending && <li className="text-sm text-ink-500">{aiCommon.working}</li>}
        <li ref={end} aria-hidden="true" />
      </ol>

      {entries.length === 0 && <AskSuggestions onPick={send} disabled={pending} />}

      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}

      <AskInput onSend={send} pending={pending} />

      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-ink-500">{askPage.notStored}</p>
        {entries.length > 0 && (
          <Button variant="ghost" size="sm" onClick={() => setEntries([])} disabled={pending}>
            {askPage.restart}
          </Button>
        )}
      </div>
    </div>
  );
}
