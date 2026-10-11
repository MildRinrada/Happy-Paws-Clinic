import { StarGlyph } from './star-glyph';

/** How much of one star an average fills: 4.8 fills four stars and 80% of the fifth. */
function share(value: number, star: number): number {
  return Math.round(Math.min(Math.max(value - (star - 1), 0), 1) * 100);
}

/**
 * An average as five stars, the last one filled only as far as the decimal reaches. Read aloud as
 * `label`, such as "คะแนนเฉลี่ย 4.8 จาก 5 ดาว".
 */
export function StarScore({ value, label }: { value: number; label: string }) {
  return (
    <span role="img" aria-label={label} className="inline-flex items-center gap-1">
      {[1, 2, 3, 4, 5].map((star) => {
        const width = share(value, star);

        return (
          <span key={star} className="relative size-[21px]">
            <StarGlyph className="absolute inset-0 size-[21px] fill-ink-200" />
            {width > 0 && (
              <span className="absolute inset-y-0 left-0 overflow-hidden" style={{ width: `${width}%` }}>
                <StarGlyph className="size-[21px] fill-stat-orange" />
              </span>
            )}
          </span>
        );
      })}
    </span>
  );
}
