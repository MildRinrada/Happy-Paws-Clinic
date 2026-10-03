import { hero } from '../../content/hero';

/** The page's one <h1>: the heading lines, the last of them ending in the accent word. */
export function HeroHeading() {
  const lastLine = hero.headingLines.length - 1;

  return (
    <h1 className="text-4xl font-bold leading-[1.25] sm:text-[44px] sm:leading-[1.25] xl:text-5xl xl:leading-[1.25]">
      {hero.headingLines.map((line, index) => (
        <span key={line} className="block">
          {line}
          {index === lastLine && <span className="text-site-orange">{hero.headingAccent}</span>}
        </span>
      ))}
    </h1>
  );
}
