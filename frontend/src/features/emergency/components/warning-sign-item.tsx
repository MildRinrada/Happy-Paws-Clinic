import type { WarningSign } from '../types/emergency.types';

/** One sign that needs a vet now: what it is, and what it looks like. */
export function WarningSignItem({ sign }: { sign: WarningSign }) {
  return (
    <li className="flex gap-3 rounded-2xl border border-site-border/60 bg-white p-4 sm:p-5">
      <span aria-hidden="true" className="mt-0.5 inline-flex size-8 shrink-0 items-center justify-center rounded-full bg-site-alert/10 text-site-alert">
        <svg viewBox="0 0 24 24" className="size-4" fill="currentColor">
          <path d="M12 2 1 21h22L12 2Zm1 15h-2v-2h2v2Zm0-4h-2V9h2v4Z" />
        </svg>
      </span>
      <div className="min-w-0">
        <p className="font-bold text-ink-900">{sign.title}</p>
        <p className="mt-1 text-[15px] leading-relaxed text-ink-700">{sign.detail}</p>
      </div>
    </li>
  );
}
