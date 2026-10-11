import { CardDivider } from '@/components/site/card-divider';
import { MetaPill } from '@/components/site/meta-pill';
import { SiteCard } from '@/components/site/site-card';
import { serviceCard } from '../../content/services-page';
import { formatDuration } from '../../lib/service-price';
import type { PublicService } from '../../types/service.types';
import { BookServiceLink } from './book-service-link';
import { PriceTierList } from './price-tier-list';
import { ServicePrice } from './service-price';

interface Props {
  service: PublicService;
  branchNames: Record<string, string>;
}

/**
 * One service: name and how long it takes, what it includes when the clinic has said, prices by
 * species and weight when there are some, which branches have it when not all do, then the price
 * and a button that opens booking with this service already chosen.
 */
export function ServiceCard({ service, branchNames }: Props) {
  const branches = service.branch_ids.map((id) => branchNames[id]).filter(Boolean);

  return (
    // A column, so the price and button sit on the card's bottom edge and line up across a row.
    <SiteCard as="li" tone="muted" className="flex flex-col">
      <div className="flex items-start justify-between gap-3">
        <h3 className="text-[17px] font-semibold leading-snug text-ink-900">{service.name}</h3>
        <MetaPill className="shrink-0">{formatDuration(service.duration_minutes)}</MetaPill>
      </div>

      {service.description && <p className="mt-3 text-[13px] leading-[22px] text-ink-700">{service.description}</p>}
      {service.price_tiers.length > 0 && <PriceTierList service={service} />}
      {branches.length > 0 && (
        <p className="mt-2 text-[13px] text-[#b57a14]">
          {serviceCard.branchesOnly} {branches.join(', ')}
        </p>
      )}

      <div className="mt-auto pt-4">
        <CardDivider />
        <div className="flex flex-wrap items-center justify-between gap-3 pt-3">
          <ServicePrice service={service} />
          <BookServiceLink service={service} />
        </div>
      </div>
    </SiteCard>
  );
}
