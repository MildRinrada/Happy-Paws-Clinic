import type { ReactNode } from 'react';
import { formatTime } from '@/lib/format/datetime';
import { headlineTiles } from '../../content/headline-tiles';
import { REFRESH_SECONDS } from '../../lib/today-settings';

/**
 * "วันนี้" with the time the numbers were counted, which moves each time the page reads itself
 * again. The time is the API's `generated_at`, so it is when the numbers were counted, not when
 * the browser drew them.
 */
export function TodaySection({ generatedAt, children }: { generatedAt: string; children: ReactNode }) {
  const text = headlineTiles;

  return (
    <section aria-labelledby="today-heading" className="space-y-3">
      <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
        <h2 id="today-heading" className="font-display text-lg font-semibold text-ink-900">
          {text.heading}
        </h2>
        <p className="text-xs text-ink-500">{text.updatedAt(formatTime(generatedAt), REFRESH_SECONDS)}</p>
      </div>
      {children}
    </section>
  );
}
