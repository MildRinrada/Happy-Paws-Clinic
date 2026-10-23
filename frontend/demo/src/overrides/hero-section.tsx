import { Suspense } from 'react';
import { SiteContainer } from '@/components/layout/site-container';
import { HeroActions } from '@/features/home/components/hero/hero-actions';
import { HeroHeading } from '@/features/home/components/hero/hero-heading';
import { SocialProof } from '@/features/home/components/hero/social-proof';
import { hero, heroImage } from '@/features/home/content/hero';

/**
 * The product's hero, laid out for the demo: the words and the two buttons on the left, the dog
 * and cat filling the right half of the page's container from top to bottom.
 *
 * The product's hero also carries the next free time and the walk-in wait. They grow with every
 * branch the clinic has, and with two branches they pushed the banner past the height the picture
 * can fill, so the demo leaves them out of the banner. Booking (`/book`) shows the free times.
 */
export function HeroSection() {
  return (
    <section className="relative overflow-hidden bg-site-teal text-white">
      <SiteContainer className="grid lg:grid-cols-2 lg:gap-8">
        <div className="relative z-10 py-12 lg:py-20">
          <HeroHeading />

          <p className="mt-7 max-w-[520px] text-base font-light leading-relaxed sm:text-lg sm:leading-[25px]">{hero.lead}</p>

          <div className="mt-8">
            <HeroActions />
          </div>

          <div className="mt-10">
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
              className="absolute inset-0 h-full w-full object-contain object-bottom lg:pt-6"
            />
          </div>
        )}
      </SiteContainer>
    </section>
  );
}
