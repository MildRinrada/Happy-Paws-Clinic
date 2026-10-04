import { Badge } from '@/components/ui/badge';
import { formatBaht } from '@/lib/format/money';
import { serviceSettings } from '../../content/service-settings';
import type { ManagedService } from '../../types/managed-service.types';

const shortDate = new Intl.DateTimeFormat('th-TH', { day: 'numeric', month: 'short', year: '2-digit', timeZone: 'UTC' });

/** What a service is today: name, state, length, price, and a price list waiting to start. */
export function ServiceSummary({ service }: { service: ManagedService }) {
  const text = serviceSettings;
  const next = service.upcoming_pricing[0];

  return (
    <div className="min-w-0 flex-1 space-y-1">
      <p className="flex flex-wrap items-center gap-2">
        <span className={`font-medium ${service.is_active ? 'text-ink-900' : 'text-ink-400 line-through'}`}>{service.name}</span>
        {!service.is_active ? (
          <Badge tone="red">{text.badges.inactive}</Badge>
        ) : service.online_booking_enabled ? (
          <Badge tone="green">{text.badges.online}</Badge>
        ) : (
          <Badge>{text.badges.counterOnly}</Badge>
        )}
        {service.pricing.tiers.length > 0 && <Badge tone="blue">{text.badges.tiers(service.pricing.tiers.length)}</Badge>}
      </p>
      <p className="text-sm text-ink-600">
        {text.minutes(service.duration_minutes)} · {formatBaht(service.price_satang)}
      </p>
      {next?.effective_from && (
        <p className="text-xs text-amber-800">
          {text.badges.upcoming(formatBaht(next.base_price_satang), shortDate.format(new Date(`${next.effective_from}T00:00:00Z`)))}
        </p>
      )}
    </div>
  );
}
