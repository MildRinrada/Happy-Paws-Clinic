import { reviewsPage } from '../../content/reviews-page';
import type { ReviewReply as Reply } from '../../types/review.types';

/** The clinic's answer, under the customer's words. */
export function ReviewReply({ reply }: { reply: Reply }) {
  return (
    <div className="mt-4 rounded-xl bg-white/70 px-4 py-3 text-sm leading-relaxed text-ink-700">
      <p className="text-xs font-semibold text-site-teal">{reviewsPage.reply}</p>
      <p className="mt-1 whitespace-pre-line break-words">{reply.text}</p>
    </div>
  );
}
