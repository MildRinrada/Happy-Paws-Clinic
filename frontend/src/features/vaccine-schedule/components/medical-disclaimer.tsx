import { vaccinePage } from '../content/vaccine-schedule-page';
import { scheduleReview } from '../content/vaccine-plans';
import { formatThaiDay } from '../lib/calendar-date';

/**
 * The warning that goes with every schedule on the page: general guidance, see the vet first. It
 * also says who at the clinic checked the schedule, or that nobody has yet.
 */
export function MedicalDisclaimer() {
  const text = vaccinePage.disclaimer;
  const { reviewedBy, reviewedOn } = scheduleReview;

  return (
    <aside role="note" className="rounded-2xl border border-site-orange/60 bg-[#fff6e8] p-4 sm:p-5">
      <p className="font-bold text-ink-900">{text.heading}</p>
      <p className="mt-1.5 text-sm leading-relaxed text-ink-700">{text.body}</p>
      <p className="mt-2 text-xs text-site-muted">
        {reviewedBy && reviewedOn
          ? text.reviewed.replace('{name}', reviewedBy).replace('{date}', formatThaiDay(reviewedOn))
          : text.pendingReview}
      </p>
    </aside>
  );
}
