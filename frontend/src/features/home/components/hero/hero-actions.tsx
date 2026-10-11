import { CtaLink } from '@/components/ui/cta-link';
import { hero } from '../../content/hero';

/** The hero's two buttons: book, and look at the services first. */
export function HeroActions() {
  return (
    <div className="flex flex-wrap gap-4 xl:gap-7">
      <CtaLink href={hero.primaryAction.href} variant="solidInk" size="lg">
        {hero.primaryAction.label}
      </CtaLink>
      <CtaLink href={hero.secondaryAction.href} size="lg" variant="glass">
        {hero.secondaryAction.label}
      </CtaLink>
    </div>
  );
}
