'use client';

import { useState, type ReactNode } from 'react';
import { labPage } from '../../content/lab-page';
import { RowExpandedContext } from '../../lib/row-expanded';

interface LabRowDisclosureProps {
  /** The request id, for the panel's id. */
  id: string;
  /** The one-line row, drawn on the server. Phrasing content only: it sits inside a button. */
  summary: ReactNode;
  /** Details, results and actions, drawn on the server. */
  children: ReactNode;
  defaultOpen: boolean;
  /** Waiting on the lab past the clinic's limit. */
  overdue: boolean;
}

/**
 * One request on the worklist: a single line that opens to the rest. The line is a real button
 * (`aria-expanded`, Enter and Space), and the panel stays mounted while closed, so closing a row
 * never throws away a form half typed in it. The state lives here, keyed by request id, which is
 * what keeps a row open across the auto refresh.
 */
export function LabRowDisclosure({ id, summary, children, defaultOpen, overdue }: LabRowDisclosureProps) {
  const [open, setOpen] = useState(defaultOpen);
  const panelId = `lab-row-${id}`;

  return (
    <li
      className={`overflow-hidden rounded-xl border bg-white ${overdue ? 'border-red-200 border-l-4 border-l-red-500' : 'border-ink-200'}`}
    >
      <button
        type="button"
        aria-expanded={open}
        aria-controls={panelId}
        onClick={() => setOpen((current) => !current)}
        className="flex w-full items-start gap-3 px-4 py-3 text-left hover:bg-ink-50 focus-visible:outline-2 focus-visible:outline-offset-[-2px] focus-visible:outline-brand-600"
      >
        <span aria-hidden className={`mt-0.5 shrink-0 text-ink-400 transition-transform ${open ? 'rotate-90' : ''}`}>
          <svg viewBox="0 0 20 20" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M7.5 4.5 13 10l-5.5 5.5" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>
        {summary}
        <span className="sr-only">{open ? labPage.row.collapse : labPage.row.expand}</span>
      </button>
      <div id={panelId} hidden={!open} className="border-t border-ink-100 px-4 py-4 sm:pl-12">
        <RowExpandedContext.Provider value={open}>{children}</RowExpandedContext.Provider>
      </div>
    </li>
  );
}
