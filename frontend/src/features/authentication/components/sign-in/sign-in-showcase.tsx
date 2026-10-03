'use client';

import { useState } from 'react';
import { signInShowcase, signInSlides } from '../../content/sign-in-slides';
import { useSlideshow } from '../../hooks/use-slideshow';
import { ShowcaseBadge } from './showcase-badge';
import { SlideBackground } from './slide-background';
import { SlideCaption } from './slide-caption';
import { SlideDots } from './slide-dots';

/**
 * The photo half of the page: a slideshow behind the logo, a caption and the slide dots.
 *
 * Shown from laptop width up. On a phone it would push the form below the fold, so the form
 * column shows a small logo instead (see `mobile-brand.tsx`).
 */
export function SignInShowcase() {
  const [paused, setPaused] = useState(false);
  const { index, goTo } = useSlideshow(signInSlides.length, signInShowcase.intervalMs, paused);
  const slide = signInSlides[index]!;

  return (
    <section
      aria-roledescription="carousel"
      aria-label={signInShowcase.carouselLabel}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocus={() => setPaused(true)}
      onBlur={() => setPaused(false)}
      className="relative hidden overflow-hidden bg-site-forest lg:block"
    >
      {signInSlides.map((item, position) => (
        <SlideBackground key={position} slide={item} active={position === index} priority={position === 0} />
      ))}

      {/* Darkens the lower part of any photo so the white caption reads on it. */}
      <div className="absolute inset-0 bg-gradient-to-t from-black/75 via-black/30 to-black/10" />

      <div className="relative flex h-full min-h-dvh flex-col px-16 pb-12 pt-12">
        <ShowcaseBadge />

        <div className="my-auto max-w-[520px] py-12">
          <SlideCaption key={index} slide={slide} />
          <SlideDots count={signInSlides.length} active={index} onSelect={goTo} />
        </div>

        <p className="text-[15px] text-white/85">{signInShowcase.footer}</p>
      </div>
    </section>
  );
}
