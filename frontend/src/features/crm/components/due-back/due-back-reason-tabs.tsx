import Link from 'next/link';
import { dueBackPage, dueBackReasons } from '../../content/due-back-page';
import { crmPaths } from '../../lib/crm-paths';
import { DUE_BACK_REASONS } from '../../lib/due-back-query';
import type { DueBackReason } from '../../types/due-back.types';

/** The two lists, as links, so the reason stays in the address. */
export function DueBackReasonTabs({ current }: { current: DueBackReason }) {
  return (
    <nav aria-label={dueBackPage.reasonsLabel} className="inline-flex flex-wrap rounded-lg border border-ink-200 bg-white p-0.5">
      {DUE_BACK_REASONS.map((reason) => (
        <Link
          key={reason}
          href={crmPaths.dueBack(reason)}
          aria-current={reason === current ? 'page' : undefined}
          className={`rounded-md px-3 py-1.5 text-sm font-medium ${
            reason === current ? 'bg-brand-50 text-brand-800 ring-1 ring-brand-200' : 'text-ink-600 hover:bg-ink-50'
          }`}
        >
          {dueBackReasons[reason].label}
        </Link>
      ))}
    </nav>
  );
}
