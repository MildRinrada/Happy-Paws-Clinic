'use client';

import { useState } from 'react';
import { assistantWidget } from '../content/assistant-widget';
import type { ChatEntry } from '../types/assistant.types';
import { ChatPanel } from './chat-panel';

/**
 * The round button in the corner and the panel it opens. The conversation lives here, in the
 * public layout, so it survives moving between pages and closing the panel; it goes when the tab does.
 */
export function ChatLauncher() {
  const [open, setOpen] = useState(false);
  const [entries, setEntries] = useState<ChatEntry[]>([]);

  return (
    <div className="fixed bottom-4 right-4 z-50 flex flex-col items-end gap-3 sm:bottom-6 sm:right-6">
      {open && (
        <section
          aria-label={assistantWidget.title}
          className="h-[min(560px,calc(100dvh-6rem))] w-[calc(100vw-2rem)] overflow-hidden rounded-2xl border border-site-border/40 bg-white shadow-2xl sm:w-[380px]"
        >
          <ChatPanel entries={entries} onEntries={setEntries} onClose={() => setOpen(false)} />
        </section>
      )}

      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-expanded={open}
        aria-label={open ? assistantWidget.close : assistantWidget.open}
        className="flex size-14 items-center justify-center rounded-full bg-site-teal text-white shadow-lg transition hover:brightness-95"
      >
        <svg viewBox="0 0 24 24" aria-hidden="true" className="size-7" fill="currentColor">
          {open ? (
            <path d="M18.3 5.7a1 1 0 0 0-1.4 0L12 10.6 7.1 5.7a1 1 0 0 0-1.4 1.4l4.9 4.9-4.9 4.9a1 1 0 1 0 1.4 1.4l4.9-4.9 4.9 4.9a1 1 0 0 0 1.4-1.4L13.4 12l4.9-4.9a1 1 0 0 0 0-1.4Z" />
          ) : (
            <path d="M12 3C6.5 3 2 6.6 2 11c0 2.4 1.3 4.6 3.4 6.1L4.6 21l4.3-2.4c1 .3 2 .4 3.1.4 5.5 0 10-3.6 10-8s-4.5-8-10-8Zm-4 9.3a1.3 1.3 0 1 1 0-2.6 1.3 1.3 0 0 1 0 2.6Zm4 0a1.3 1.3 0 1 1 0-2.6 1.3 1.3 0 0 1 0 2.6Zm4 0a1.3 1.3 0 1 1 0-2.6 1.3 1.3 0 0 1 0 2.6Z" />
          )}
        </svg>
      </button>
    </div>
  );
}
