import { Badge } from '@/components/ui/badge';
import { reviewStatusLabels, reviewStatusTones } from '../../content/staff-reviews';
import type { ReviewStatus } from '../../types/review.types';

/** Where a review stands: waiting, on the site, or kept off. */
export function ReviewStatusBadge({ status }: { status: ReviewStatus }) {
  return <Badge tone={reviewStatusTones[status]}>{reviewStatusLabels[status]}</Badge>;
}
