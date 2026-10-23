import { seedArticles } from './articles';
import { seedReviews } from './reviews';

/**
 * After the clinic (0), before customers (10): the reviews are tied to real customers on the
 * first read instead (see `linkReviews`).
 */
export const order = 5;

export function seed() {
  seedArticles();
  seedReviews();
}
