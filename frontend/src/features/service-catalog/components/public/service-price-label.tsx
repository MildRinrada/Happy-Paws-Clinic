import { serviceCard } from '../../content/services-page';
import { formatMenuPrice, priceRange } from '../../lib/service-price';
import type { PublicService } from '../../types/service.types';

/** The price beside a service's name: "฿500", or "เริ่มต้น ฿400" when it depends on the animal. */
export function ServicePriceLabel({ service }: { service: PublicService }) {
  const { min, max } = priceRange(service);

  if (max === 0) {
    return <p className="shrink-0 text-sm font-bold text-site-muted">{serviceCard.askPrice}</p>;
  }

  return (
    <p className="shrink-0 text-right font-bold text-site-teal">
      {min !== max && <span className="mr-1 text-xs font-medium text-site-muted">{serviceCard.startingAt}</span>}
      {formatMenuPrice(min)}
    </p>
  );
}
