import { serviceCard } from '../../content/services-page';
import { describeTier, formatMenuPrice } from '../../lib/service-price';
import type { PublicService } from '../../types/service.types';

/** The price for each species or weight the clinic priced separately, then the price for the rest. */
export function PriceTierList({ service }: { service: PublicService }) {
  return (
    <div className="mt-4 rounded-xl bg-site-soft px-4 py-3">
      <p className="text-[13px] font-semibold">{serviceCard.tiersHeading}</p>
      <dl className="mt-1.5 space-y-1 text-[13px]">
        {service.price_tiers.map((tier, index) => (
          <div key={index} className="flex justify-between gap-3">
            <dt className="text-[#555555]">{describeTier(tier)}</dt>
            <dd className="shrink-0 font-semibold">{formatMenuPrice(tier.price_satang)}</dd>
          </div>
        ))}
        <div className="flex justify-between gap-3">
          <dt className="text-[#555555]">{serviceCard.otherwise}</dt>
          <dd className="shrink-0 font-semibold">{formatMenuPrice(service.price_satang)}</dd>
        </div>
      </dl>
    </div>
  );
}
