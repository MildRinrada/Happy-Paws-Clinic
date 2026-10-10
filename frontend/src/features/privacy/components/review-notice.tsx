import { reviewNotice } from '../content/privacy-policy';

/**
 * Shown while the published version is not marked as reviewed by a lawyer. The owner says whether
 * it was when publishing (`legal_review_done`); a later version can say so without editing this one.
 */
export function ReviewNotice() {
  return (
    <aside role="note" className="rounded-2xl border border-amber-200 bg-amber-50 p-5 text-amber-900">
      <p className="font-semibold">{reviewNotice.title}</p>
      <p className="mt-1 text-[15px] leading-relaxed text-amber-900/85">{reviewNotice.body}</p>
    </aside>
  );
}
