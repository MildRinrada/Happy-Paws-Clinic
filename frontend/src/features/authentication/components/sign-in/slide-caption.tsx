import { signInShowcase, type SignInSlide } from '../../content/sign-in-slides';

/**
 * The active slide's words: the tagline, the heading and the lines under it. Keyed by slide in
 * the parent, so each new slide's text rises in afresh.
 */
export function SlideCaption({ slide }: { slide: SignInSlide }) {
  return (
    <div className="animate-caption-in motion-reduce:animate-none">
      <p className="text-sm tracking-[0.2em] text-white/85">{signInShowcase.tagline}</p>

      <h2 className="mt-5 font-display text-[34px] font-bold leading-[1.4] text-white">
        {slide.heading.map((line) => (
          <span key={line} className="block">
            {line}
          </span>
        ))}
      </h2>

      <p className="mt-3 text-base leading-loose text-white/85">
        {slide.body.map((line) => (
          <span key={line} className="block">
            {line}
          </span>
        ))}
      </p>
    </div>
  );
}
