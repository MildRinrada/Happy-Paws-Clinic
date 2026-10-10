import { Badge } from '@/components/ui/badge';
import { purchaseOrderStatusLabels } from '../content/purchase-order-list';
import type { PurchaseOrderStatus } from '../types/inventory.types';

/** Where an order stands, as a coloured label. */
export function PurchaseOrderStatusBadge({ status }: { status: PurchaseOrderStatus }) {
  const label = purchaseOrderStatusLabels[status];

  return <Badge tone={label.tone}>{label.text}</Badge>;
}
