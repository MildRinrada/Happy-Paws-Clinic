import { Badge } from '@/components/ui/badge';
import { customerList } from '../content/customer-list';

/** Whether the customer can see their own pets online. */
export function PortalStatusBadge({ linked }: { linked: boolean }) {
  return linked ? (
    <Badge tone="green">{customerList.portal.yes}</Badge>
  ) : (
    <Badge>{customerList.portal.no}</Badge>
  );
}
