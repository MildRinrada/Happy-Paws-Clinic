'use client';

import { useEffect, useRef, useState, useTransition, type FormEvent } from 'react';
import { sendChatAction } from '../actions/assistant-actions';
import { assistantWidget } from '../content/assistant-widget';
import type { ChatEntry } from '../types/assistant.types';
import { ChatBubble } from './chat-bubble';
import { TypingDots } from './typing-dots';

/** The API takes at most 20 messages; older ones fall off the front, the conversation carries on. */
const HISTORY = 20;
const MAX_LENGTH = 1000;

interface ChatPanelProps {
  entries: ChatEntry[];
  onEntries: (entries: ChatEntry[]) => void;
  onClose: () => void;
}

/**
 * The conversation: messages, suggested first questions, and the box to type in. The whole
 * conversation is sent with each message and kept nowhere but here.
 */
export function ChatPanel({ entries, onEntries, onClose }: ChatPanelProps) {
  const text = assistantWidget;
  const [draft, setDraft] = useState('');
  const [error, setError] = useState<string>();
  const [pending, startTransition] = useTransition();
  const list = useRef<HTMLOListElement>(null);
  const input = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    list.current?.scrollTo({ top: list.current.scrollHeight, behavior: 'smooth' });
  }, [entries, pending]);

  useEffect(() => input.current?.focus(), []);

  function send(question: string) {
    const message = question.trim().slice(0, MAX_LENGTH);
    if (!message || pending) return;

    const next: ChatEntry[] = [...entries, { role: 'user', text: message }];
    onEntries(next);
    setDraft('');
    setError(undefined);

    startTransition(async () => {
      try {
        const history = next.slice(-HISTORY).map(({ role, text: body }) => ({ role, text: body }));
        const result = await sendChatAction(history);

        if (result.ok) {
          onEntries([...next, { role: 'assistant', text: result.data.reply, actions: result.data.actions }]);
        } else {
          setError(result.error.message);
        }
      } catch {
        setError(text.unreachable);
      }
    });
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    send(draft);
  }

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-start justify-between gap-3 bg-site-forest px-4 py-3 text-white">
        <div>
          <p className="font-semibold">{text.title}</p>
          <p className="text-xs text-white/80">{text.subtitle}</p>
        </div>
        <div className="flex items-center gap-1">
          {entries.length > 0 && (
            <button type="button" onClick={() => onEntries([])} className="rounded-md px-2 py-1 text-xs text-white/85 hover:bg-white/10">
              {text.restart}
            </button>
          )}
          <button type="button" onClick={onClose} aria-label={text.close} className="rounded-md px-2 py-1 text-lg leading-none hover:bg-white/10">
            ×
          </button>
        </div>
      </header>

      <ol ref={list} className="flex-1 space-y-3 overflow-y-auto px-4 py-4" aria-live="polite">
        <ChatBubble entry={{ role: 'assistant', text: text.greeting }} onNavigate={onClose} />
        {entries.map((entry, index) => (
          <ChatBubble key={index} entry={entry} onNavigate={onClose} />
        ))}
        {pending && <TypingDots />}
      </ol>

      {entries.length === 0 && (
        <div className="flex flex-wrap gap-1.5 px-4 pb-2">
          {text.suggestions.map((suggestion) => (
            <button
              key={suggestion}
              type="button"
              onClick={() => send(suggestion)}
              className="rounded-full bg-site-soft px-3 py-1.5 text-sm text-ink-800 transition hover:bg-site-teal/15"
            >
              {suggestion}
            </button>
          ))}
        </div>
      )}

      {error && <p role="alert" className="px-4 pb-2 text-sm text-site-alert">{error}</p>}

      <form onSubmit={submit} className="flex items-end gap-2 border-t border-site-border/40 p-3">
        <label htmlFor="assistant-input" className="sr-only">
          {text.placeholder}
        </label>
        <textarea
          id="assistant-input"
          ref={input}
          value={draft}
          rows={1}
          maxLength={MAX_LENGTH}
          placeholder={text.placeholder}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter' && !event.shiftKey && !event.nativeEvent.isComposing) {
              event.preventDefault();
              send(draft);
            }
          }}
          className="max-h-28 min-h-10 flex-1 resize-none rounded-xl border border-site-border/60 px-3 py-2 text-[15px] outline-none focus:border-site-teal"
        />
        <button
          type="submit"
          disabled={pending || draft.trim() === ''}
          className="h-10 rounded-xl bg-site-teal px-4 font-semibold text-white transition hover:brightness-95 disabled:opacity-50"
        >
          {text.send}
        </button>
      </form>

      <p className="px-4 pb-3 text-[11px] leading-snug text-site-muted">{text.disclaimer}</p>
    </div>
  );
}
