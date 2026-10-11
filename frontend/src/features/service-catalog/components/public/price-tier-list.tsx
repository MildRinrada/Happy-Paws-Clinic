import { serviceCard } from '../../content/services-page';
import { describeTier, formatMenuPrice } from '../../lib/service-price';
import type { PublicService } from '../../types/service.types';

/** The price for each species or weight the clinic priced separately, then the price for the rest. */
export function PriceTierList({ service }: { service: PublicService }) {
  return (
    <div className="mt-3 text-[13px] leading-[19px]">
      <p className="text-ink-600">{serviceCard.tiersHeading}</p>
      <dl className="mt-1 space-y-0.5">
        {service.price_tiers.map((tier, index) => (
          <div key={index} className="flex justify-between gap-3">
            <dt className="text-ink-700">{describeTier(tier)}</dt>
            <dd className="shrink-0 font-semibold text-site-forest">{formatMenuPrice(tier.price_satang)}</dd>
          </div>
        ))}
        <div className="flex justify-between gap-3">
          <dt className="text-ink-700">{serviceCard.otherwise}</dt>
          <dd className="shrink-0 font-semibold text-site-forest">{formatMenuPrice(service.price_satang)}</dd>
        </div>
      </dl>
    </div>
  );
}
