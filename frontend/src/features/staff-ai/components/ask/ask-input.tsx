'use client';

import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { askPage } from '../../content/ask-page';

const MAX_LENGTH = 1000;

/** The question box. Enter sends, Shift+Enter starts a new line. */
export function AskInput({ onSend, pending }: { onSend: (question: string) => void; pending: boolean }) {
  const id = useId();
  const [draft, setDraft] = useState('');

  function send() {
    if (pending || draft.trim() === '') return;
    onSend(draft);
    setDraft('');
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    send();
  }

  return (
    <form onSubmit={submit} className="flex items-end gap-2">
      <label htmlFor={id} className="sr-only">
        {askPage.placeholder}
      </label>
      <textarea
        id={id}
        value={draft}
        rows={2}
        maxLength={MAX_LENGTH}
        placeholder={askPage.placeholder}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
            event.preventDefault();
            send();
          }
        }}
        className="min-h-10 min-w-0 flex-1 resize-none rounded-lg border border-ink-200 px-3 py-2 text-sm outline-none focus:border-brand-500"
      />
      <Button type="submit" disabled={pending || draft.trim() === ''}>
        {askPage.send}
      </Button>
    </form>
  );
}
