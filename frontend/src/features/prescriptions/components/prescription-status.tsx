import { Badge } from '@/components/ui/badge';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { PrescriptionItem } from '../types/prescription.types';

/**
 * Where a prescription stands: waiting or handed over, still being taken, and duplicated. While it
 * waits, also whether the drug was picked from stock or typed, which decides what the dispense
 * takes and charges (docs/contract-extensions.md § 20).
 */
export function PrescriptionStatus({ item }: { item: PrescriptionItem }) {
  const text = prescriptionsPage;

  return (
    <>
      {item.dispensed_at ? (
        <Badge tone="green">{text.status.dispensed}</Badge>
      ) : (
        <Badge tone="amber">{text.status.waiting}</Badge>
      )}
      {!item.dispensed_at &&
        (item.inventory_item_id ? (
          <Badge tone="blue">{text.stock.linked}</Badge>
        ) : (
          <Badge tone="neutral">{text.stock.typed}</Badge>
        ))}
      {item.dispensed_at && item.stock_deducted === false && <Badge tone="amber">{text.stock.historyNotDeducted}</Badge>}
      {item.active && <Badge tone="blue">{text.status.active}</Badge>}
      {item.duplicate_of.length > 0 && <Badge tone="red">{text.duplicate.badge}</Badge>}
    </>
  );
}
