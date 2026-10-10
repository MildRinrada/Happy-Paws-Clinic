import { Suspense } from 'react';
import { WalkInWait } from '@/features/wait-estimate/components/walk-in-wait';
import { hero } from '../../content/hero';
import { NextFreeTime } from '../next-free/next-free-time';
import { SectionContainer } from '../shared/section-container';
import { HeroActions } from './hero-actions';
import { HeroHeading } from './hero-heading';
import { HeroImage } from './hero-image';
import { SocialProof } from './social-proof';

/**
 * The opening of the page: what the clinic is, the two things to do next, the earliest free time
 * to book, the walk-in wait now, and the banner. The free time and the wait each stream in on their
 * own, so a slow read never holds up the rest of the page, and leave no gap when there is nothing
 * to show.
 */
export function HeroSection() {
  return (
    <section className="relative overflow-hidden bg-site-teal text-white">
      <SectionContainer>
        {/* The text keeps to the left so the banner has the right of the section to itself. */}
        <div className="relative z-10 py-12 lg:w-[54%] lg:py-16 xl:w-[58%] xl:pb-[72px] xl:pt-[76px]">
          <HeroHeading />

          <p className="mt-7 max-w-[520px] text-base font-light leading-relaxed sm:text-lg sm:leading-[25px] xl:mt-9">
            {hero.lead}
          </p>

          <div className="mt-8 xl:mt-9">
            <HeroActions />
          </div>

          <Suspense fallback={null}>
            <NextFreeTime />
          </Suspense>

          <Suspense fallback={null}>
            <WalkInWait tone="hero" className="mt-4 max-w-[520px]" />
          </Suspense>

          <div className="mt-8">
            <Suspense fallback={null}>
              <SocialProof />
            </Suspense>
          </div>
        </div>
      </SectionContainer>

      <HeroImage />
    </section>
  );
}
