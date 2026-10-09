'use client';

import { useId, useRef, type ReactNode } from 'react';
import { Button } from '@/components/ui/button';

interface Props {
  trigger: string;
  title: string;
  /** Called as the dialog opens, so the form inside can start from the ticket as it is now. */
  onOpen?: () => void;
  /** The body. Receives `close` so a form can shut the dialog once its save went through. */
  children: (close: () => void) => ReactNode;
}

/** A small modal behind a button on a ticket. The native `<dialog>` keeps focus and Esc for free. */
export function QueueDialog({ trigger, title, onOpen, children }: Props) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const close = () => dialog.current?.close();

  return (
    <>
      <Button
        size="sm"
        variant="ghost"
        onClick={() => {
          onOpen?.();
          dialog.current?.showModal();
        }}
      >
        {trigger}
      </Button>

      <dialog
        ref={dialog}
        aria-labelledby={titleId}
        // m-auto: Tailwind's reset zeroes the margin a modal <dialog> centres itself with.
        className="m-auto w-full max-w-md rounded-2xl border border-ink-200 p-0 backdrop:bg-ink-900/40"
      >
        <div className="space-y-4 p-5">
          <h2 id={titleId} className="font-display text-lg font-semibold text-ink-900">
            {title}
          </h2>
          {children(close)}
        </div>
      </dialog>
    </>
  );
}
