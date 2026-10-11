import { SiteCard } from '@/components/site/site-card';
import { emergencyPage } from '../content/emergency-page';
import type { CallNumber } from '../types/emergency.types';

/** One number as one big button: the whole card dials it. */
export function CallCard({ number }: { number: CallNumber }) {
  const text = emergencyPage.call;
  // The clinic's own name for the line ("สายด่วน 24 ชม."), or "เบอร์คลินิก" for the stand-in.
  const name = number.fallback ? text.fallback : number.label || text.emergency;
  // The branch is added unless the clinic's label already names it.
  const caption = number.branchName && !name.includes(number.branchName) ? `${name} ${number.branchName}` : name;

  return (
    <li>
      <SiteCard
        href={number.href}
        tone="raised"
        padding="none"
        ariaLabel={text.callLabel(caption, number.value)}
        className="flex flex-wrap items-center justify-between gap-x-4 gap-y-3 px-5 py-4 text-site-alert hover:bg-white/90 sm:px-6"
      >
        <span className="min-w-0">
          <span className="block text-sm font-semibold text-ink-700">{caption}</span>
          <span className="block text-3xl font-bold tabular-nums [overflow-wrap:anywhere] sm:text-4xl">{number.value}</span>
        </span>
        <span className="inline-flex h-11 items-center gap-2 rounded-xl bg-site-alert px-5 font-bold text-white">
          <svg viewBox="0 0 24 24" aria-hidden="true" className="size-5" fill="currentColor">
            <path d="M6.6 10.8a15.1 15.1 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.25 11.4 11.4 0 0 0 3.6.57 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1c0 1.25.2 2.45.57 3.57a1 1 0 0 1-.25 1L6.6 10.8Z" />
          </svg>
          {text.action}
        </span>
      </SiteCard>
    </li>
  );
}
