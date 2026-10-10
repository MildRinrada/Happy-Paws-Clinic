import { loadPublicReviews } from '@/features/reviews/lib/load-public-reviews';
import { hero } from '../../content/hero';
import { AvatarStack } from './avatar-stack';

/**
 * How many pets the clinic has looked after, and what their families said about it. The review
 * score is the published reviews' own summary; with none published, or the API away, the line is
 * left out rather than shown with a made-up figure.
 */
export async function SocialProof() {
  const { badge, petsLine, reviewsLine } = hero.socialProof;
  const summary = (await loadPublicReviews(1))?.summary;
  const lines = summary && summary.count > 0 && typeof summary.average === 'number'
    ? [petsLine, reviewsLine(summary.average, summary.count)]
    : [petsLine];

  return (
    <div className="flex items-center gap-5 xl:gap-6 xl:pl-4">
      <AvatarStack badge={badge} />
      <p className="text-[15px] font-light leading-snug sm:text-[17px] sm:leading-[22px]">
        {lines.map((line) => (
          <span key={line} className="block">
            {line}
          </span>
        ))}
      </p>
    </div>
  );
}
