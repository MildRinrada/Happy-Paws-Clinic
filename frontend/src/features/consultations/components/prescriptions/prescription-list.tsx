import { Badge } from '@/components/ui/badge';
import { consultationPage } from '../../content/consultation-page';
import type { Prescription } from '../../types/consultation.types';

/** What was prescribed, and any allergy the vet prescribed over, with their reason. */
export function PrescriptionList({ prescriptions }: { prescriptions: Prescription[] }) {
  const text = consultationPage.prescriptions;

  if (prescriptions.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <ul className="divide-y divide-ink-100 text-sm">
      {prescriptions.map((item) => (
        <li key={item.id} className="space-y-0.5 py-2">
          <p className="flex flex-wrap items-center gap-2">
            <span className="font-medium text-ink-900">{item.drug_name}</span>
            {item.dispensed_at && <Badge tone="green">{text.dispensed}</Badge>}
          </p>
          <p className="text-ink-700">{text.line(item.dose, item.frequency, item.duration_days, item.quantity)}</p>
          {item.instructions && <p className="text-ink-600">{item.instructions}</p>}
          {item.allergy_acknowledgement && (
            <p className="text-amber-800">{text.acknowledged(item.allergy_acknowledgement)}</p>
          )}
        </li>
      ))}
    </ul>
  );
}
