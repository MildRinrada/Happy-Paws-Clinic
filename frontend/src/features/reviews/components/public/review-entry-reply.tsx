import { reviewsPage } from '../../content/reviews-page';
import type { ReviewReply } from '../../types/review.types';

/** The clinic's answer, in a grey box inside a white review card. */
export function ReviewEntryReply({ reply }: { reply: ReviewReply }) {
  return (
    <div className="mt-3 rounded-xl bg-site-soft px-4 py-3.5 sm:px-[18px]">
      <p className="text-[13px] font-bold text-site-forest">{reviewsPage.reply}</p>
      <p className="mt-1 whitespace-pre-line break-words text-sm leading-relaxed text-ink-700">{reply.text}</p>
    </div>
  );
}
