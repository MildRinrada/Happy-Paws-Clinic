import type { LabRequest } from '../types/lab.types';
import { ResultForm } from './results/result-form';
import { ReviewButton } from './review-button';

interface LabRequestActionsProps {
  request: LabRequest;
  /** `lab:write`: the lab enters results. */
  canRecord: boolean;
  /** `lab:read`: a vet marks results as read. */
  canReview: boolean;
}

/** What can be done to a request from here: mark its results read, or enter more. */
export function LabRequestActions({ request, canRecord, canReview }: LabRequestActionsProps) {
  const review = canReview && request.status === 'resulted';
  if (!review && !canRecord) return null;

  return (
    <div className="space-y-3">
      {review && <ReviewButton labRequestId={request.id} />}
      {canRecord && <ResultForm labRequestId={request.id} hasResults={request.results.length > 0} />}
    </div>
  );
}
