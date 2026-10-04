import { reviewNotice } from '../content/privacy-policy';

/**
 * The visible reminder that this policy is a draft written from the system, not from a lawyer.
 * Remove it from the page once the clinic's legal adviser has signed the text off.
 */
export function ReviewNotice() {
  return (
    <aside role="note" className="rounded-2xl border border-amber-200 bg-amber-50 p-5 text-amber-900">
      <p className="font-semibold">{reviewNotice.title}</p>
      <p className="mt-1 text-[15px] leading-relaxed text-amber-900/85">{reviewNotice.body}</p>
    </aside>
  );
}
