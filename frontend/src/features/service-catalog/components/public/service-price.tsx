import { serviceCard } from '../../content/services-page';
import { formatMenuPrice, priceRange } from '../../lib/service-price';
import type { PublicService } from '../../types/service.types';

/** The price at the bottom of a service card: "เริ่มต้น ฿450", or "สอบถามราคา" when there is none. */
export function ServicePrice({ service }: { service: PublicService }) {
  const { min, max } = priceRange(service);

  if (max === 0) {
    return <p className="text-[15px] font-semibold text-ink-600">{serviceCard.askPrice}</p>;
  }

  return (
    <p className="flex items-baseline gap-1.5 whitespace-nowrap">
      <span className="text-[13px] text-ink-600">{serviceCard.startingAt}</span>
      <span className="text-xl font-bold text-site-forest">{formatMenuPrice(min)}</span>
    </p>
  );
}
