import { SiteCard } from '@/components/site/site-card';
import type { PolicyDefinition } from '../types/privacy-policy.types';

/**
 * Terms with an explanation under each, e.g. a kind of data and what it covers. Stacked rather
 * than a table, so it reads the same on a phone with no sideways scrolling.
 */
export function PolicyDefinitions({ items }: { items: PolicyDefinition[] }) {
  return (
    <SiteCard as="dl" tone="whiteSoft" padding="none" className="divide-y divide-site-border/60">
      {items.map((item) => (
        <div key={item.term} className="p-4 sm:p-5">
          <dt className="break-words font-semibold text-ink-900">{item.term}</dt>
          <dd className="mt-1 text-[15px] leading-relaxed text-ink-700">{item.detail}</dd>
        </div>
      ))}
    </SiteCard>
  );
}
