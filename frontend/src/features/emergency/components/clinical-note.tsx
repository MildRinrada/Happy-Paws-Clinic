import { SiteCard } from '@/components/site/site-card';
import { formatThaiDay } from '@/features/vaccine-schedule/lib/calendar-date';
import { clinicalReview } from '../content/emergency-care';
import { emergencyPage } from '../content/emergency-page';

/**
 * The note under the advice: general guidance, not a diagnosis. Like the vaccine schedule page's
 * note, it also says which clinic vet checked the advice, or that nobody has yet.
 */
export function ClinicalNote() {
  const text = emergencyPage.note;
  const { reviewedBy, reviewedOn } = clinicalReview;

  return (
    <SiteCard as="aside" role="note" tone="notice" padding="sm">
      <p className="font-bold text-ink-900">{text.heading}</p>
      <p className="mt-1.5 text-sm leading-relaxed text-ink-700">{text.body}</p>
      <p className="mt-2 text-xs text-site-muted">
        {reviewedBy && reviewedOn
          ? text.reviewed.replace('{name}', reviewedBy).replace('{date}', formatThaiDay(reviewedOn))
          : text.pendingReview}
      </p>
    </SiteCard>
  );
}
