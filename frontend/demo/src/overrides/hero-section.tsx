import { Suspense } from 'react';
import { SiteContainer } from '@/components/layout/site-container';
import { HeroActions } from '@/features/home/components/hero/hero-actions';
import { HeroHeading } from '@/features/home/components/hero/hero-heading';
import { SocialProof } from '@/features/home/components/hero/social-proof';
import { NextFreeTime } from '@/features/home/components/next-free/next-free-time';
import { hero, heroImage } from '@/features/home/content/hero';
import { WalkInWait } from '@/features/wait-estimate/components/walk-in-wait';

/**
 * The product's hero, laid out for the demo. In the product the banner hangs off the section's
 * right edge at 82% of its height; with the next free time and the walk-in wait both filled in,
 * the section grows so tall that the dog and cat dwarf the text. Here the two halves sit side by
 * side inside the page's container, with tighter spacing, and the picture fills its half down to
 * the bottom edge. Same parts, same words, same data.
 */
export function HeroSection() {
  return (
    <section className="demo-hero relative overflow-hidden bg-site-teal text-white">
      <SiteContainer className="grid lg:grid-cols-2 lg:gap-8">
        <div className="relative z-10 py-10 lg:py-11">
          <HeroHeading />

          <p className="mt-4 max-w-[520px] text-base font-light leading-relaxed sm:text-lg sm:leading-[25px]">{hero.lead}</p>

          <div className="mt-5">
            <HeroActions />
          </div>

          <div className="demo-hero__live max-w-[520px]">
            <Suspense fallback={null}>
              <NextFreeTime />
            </Suspense>
            <Suspense fallback={null}>
              <WalkInWait tone="hero" className="mt-3" />
            </Suspense>
          </div>

          <div className="mt-5">
            <Suspense fallback={null}>
              <SocialProof />
            </Suspense>
          </div>
        </div>

        {heroImage.src && (
          <div className="relative -mx-5 h-64 sm:-mx-6 sm:h-80 lg:mx-0 lg:h-auto">
            <img
              src={heroImage.src}
              alt={heroImage.alt}
              fetchPriority="high"
              className="absolute inset-0 h-full w-full object-contain object-bottom"
            />
          </div>
        )}
      </SiteContainer>
    </section>
  );
}
