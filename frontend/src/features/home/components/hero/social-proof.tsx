import { hero } from '../../content/hero';
import { AvatarStack } from './avatar-stack';

/** How many pets the clinic has looked after, and what their families said about it. */
export function SocialProof() {
  const { badge, lines } = hero.socialProof;

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
