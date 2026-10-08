import { Badge, type BadgeTone } from '@/components/ui/badge';
import type { DrugStockStatus } from '@/features/prescriptions/types/drug-option.types';
import { drugPicker } from '../../content/drug-picker';

const tones: Record<DrugStockStatus, BadgeTone> = { ok: 'green', low: 'amber', out: 'red' };

/** Whether the case's branch has the drug: มีของ, ใกล้หมด or หมด. */
export function StockStatusBadge({ status }: { status: DrugStockStatus }) {
  return <Badge tone={tones[status]}>{drugPicker.status[status]}</Badge>;
}
