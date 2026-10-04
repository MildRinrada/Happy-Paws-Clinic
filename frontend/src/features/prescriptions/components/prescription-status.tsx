import { Badge } from '@/components/ui/badge';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { PrescriptionItem } from '../types/prescription.types';

/** Where a prescription stands: waiting or handed over, still being taken, and duplicated. */
export function PrescriptionStatus({ item }: { item: PrescriptionItem }) {
  const text = prescriptionsPage;

  return (
    <>
      {item.dispensed_at ? (
        <Badge tone="green">{text.status.dispensed}</Badge>
      ) : (
        <Badge tone="amber">{text.status.waiting}</Badge>
      )}
      {item.active && <Badge tone="blue">{text.status.active}</Badge>}
      {item.duplicate_of.length > 0 && <Badge tone="red">{text.duplicate.badge}</Badge>}
    </>
  );
}
