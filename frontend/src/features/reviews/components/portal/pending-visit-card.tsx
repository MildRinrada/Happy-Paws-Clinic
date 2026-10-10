'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { portalReviews } from '../../content/portal-reviews';
import { formatVisitDay } from '../../lib/review-dates';
import type { PendingVisit, SubmittedReview } from '../../types/review.types';
import { ReviewDone } from '../shared/review-done';
import { PortalReviewForm } from './portal-review-form';

interface PendingVisitCardProps {
  visit: PendingVisit;
  firstName?: string | null;
  /** Opened on arrival: the one the home card linked to. */
  startOpen?: boolean;
}

/** One visit waiting for a review: what it was, then the form once the owner chooses to write. */
export function PendingVisitCard({ visit, firstName, startOpen = false }: PendingVisitCardProps) {
  const text = portalReviews;
  const [open, setOpen] = useState(startOpen);
  const [done, setDone] = useState<SubmittedReview>();

  return (
    <li className="space-y-4 rounded-2xl bg-white p-5 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="break-words text-base font-semibold text-ink-900">
            {text.visitTitle(visit.pet_name, formatVisitDay(visit.visit_on))}
          </p>
          <p className="break-words text-sm text-site-muted">
            {[visit.service_name, visit.branch_name].filter(Boolean).join(' | ')}
          </p>
          <p className="text-xs text-site-muted">{text.until(formatVisitDay(visit.reviewable_until))}</p>
        </div>
        {!open && !done && (
          <Button size="sm" onClick={() => setOpen(true)}>
            {text.open}
          </Button>
        )}
      </div>

      {done ? (
        <ReviewDone review={done} />
      ) : (
        open && <PortalReviewForm visitId={visit.visit_id} firstName={firstName} onDone={setDone} />
      )}
    </li>
  );
}
