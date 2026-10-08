import { Badge } from '@/components/ui/badge';
import { stockStatusLabels } from '../content/inventory-labels';
import type { StockStatus } from '../types/inventory.types';

export function StockStatusBadge({ status }: { status: StockStatus }) {
  const { label, tone } = stockStatusLabels[status];

  return <Badge tone={tone}>{label}</Badge>;
}
