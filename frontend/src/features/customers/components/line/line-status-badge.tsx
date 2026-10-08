import { Badge } from '@/components/ui/badge';
import { customerLine } from '../../content/customer-line';
import type { CustomerLine } from '../../types/customer.types';

/** Linked and reachable, linked but blocked, or not linked. */
export function LineStatusBadge({ line }: { line: CustomerLine }) {
  if (!line.linked) return <Badge>{customerLine.none}</Badge>;

  return line.can_receive ? <Badge tone="green">{customerLine.linked}</Badge> : <Badge tone="amber">{customerLine.blocked}</Badge>;
}
