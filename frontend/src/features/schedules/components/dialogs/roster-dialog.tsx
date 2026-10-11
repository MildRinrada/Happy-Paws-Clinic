'use client';

import { useEffect, useId, useRef, type ReactNode } from 'react';
import { schedulePage } from '../../content/schedule-page';

interface RosterDialogProps {
  open: boolean;
  title: string;
  /** Called on Esc, on the close button, and when the dialog is shut from inside. */
  onClose: () => void;
  children: ReactNode;
}

/**
 * The roster's modal, opened from state rather than from a button of its own, so a cell, a chip
 * or the page header can all open it. The native `<dialog>` keeps focus and Esc, as the queue's
 * dialogs do. The body is only mounted while open, so each opening starts a fresh form.
 */
export function RosterDialog({ open, title, onClose, children }: RosterDialogProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const titleId = useId();

  useEffect(() => {
    const element = dialog.current;
    if (!element) return;
    if (open && !element.open) element.showModal();
    if (!open && element.open) element.close();
  }, [open]);

  return (
    <dialog
      ref={dialog}
      aria-labelledby={titleId}
      onClose={onClose}
      // m-auto: Tailwind's reset zeroes the margin a modal <dialog> centres itself with.
      className="m-auto w-[calc(100%-2rem)] max-w-lg rounded-2xl border border-ink-200 p-0 backdrop:bg-ink-900/40"
    >
      <div className="max-h-[85vh] space-y-4 overflow-y-auto p-5">
        <div className="flex items-start justify-between gap-3">
          <h2 id={titleId} className="font-display text-lg font-semibold text-ink-900">
            {title}
          </h2>
          <button
            type="button"
            aria-label={schedulePage.dialogs.close}
            className="inline-flex size-8 shrink-0 items-center justify-center rounded-md text-ink-500 hover:bg-ink-100 hover:text-ink-800"
            onClick={onClose}
          >
            <span aria-hidden="true">×</span>
          </button>
        </div>
        {open && children}
      </div>
    </dialog>
  );
}
