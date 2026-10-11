import { ChipNav } from '@/components/site/chip-nav';
import { DotChip } from '@/components/site/dot-chip';
import { reviewFilters } from '../../content/reviews-page';
import { REVIEW_FILTERS } from '../../lib/review-filters';
import { reviewListHref } from '../../lib/review-list-href';
import type { ReviewFilter } from '../../types/review-list.types';

/**
 * "ทั้งหมด", "5 ดาว", "4 ดาว", "3 ดาวลงไป" and "มีคำตอบจากคลินิก", the one on screen filled dark
 * green. Plain links to `?filter=`, so they work before any script loads. Each starts from the
 * newest review.
 */
export function ReviewFilterNav({ current }: { current: ReviewFilter }) {
  return (
    <ChipNav label={reviewFilters.label} layout="inline">
      {REVIEW_FILTERS.map((filter) => (
        <DotChip
          key={filter}
          href={reviewListHref({ filter })}
          label={reviewFilters.chips[filter]}
          current={filter === current}
        />
      ))}
    </ChipNav>
  );
}
