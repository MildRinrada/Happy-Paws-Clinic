import Image from 'next/image';
import type { SignInSlide } from '../../content/sign-in-slides';

interface SlideBackgroundProps {
  slide: SignInSlide;
  active: boolean;
  /** Only the first slide's picture is fetched ahead of everything else. */
  priority: boolean;
}

/**
 * One slide's picture, filling the whole panel. All slides are stacked and the active one fades
 * in over the others.
 *
 * With no picture set yet, the slide is the clinic's green, and says what picture belongs there.
 */
export function SlideBackground({ slide, active, priority }: SlideBackgroundProps) {
  return (
    <div
      aria-hidden={!active}
      className={`absolute inset-0 transition-opacity duration-1000 motion-reduce:transition-none ${active ? 'opacity-100' : 'opacity-0'}`}
    >
      {slide.image ? (
        <Image
          src={slide.image}
          alt={slide.imageAlt}
          fill
          priority={priority}
          sizes="50vw"
          className="object-cover"
          style={{ objectPosition: slide.focus ?? 'center' }}
        />
      ) : (
        <div className="flex h-full items-start justify-center bg-gradient-to-br from-site-teal to-site-forest pt-[22%]">
          <span className="rounded-full border border-dashed border-white/40 px-4 py-1.5 text-sm text-white/70">
            {slide.imageLabel}
          </span>
        </div>
      )}
    </div>
  );
}
