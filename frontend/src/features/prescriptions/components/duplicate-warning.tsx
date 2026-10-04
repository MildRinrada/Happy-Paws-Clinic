import { formatDateTime } from '@/lib/format/datetime';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { PrescriptionItem } from '../types/prescription.types';

interface Props {
  /** The overlapping prescriptions that are on this page. */
  others: PrescriptionItem[];
  /** Overlapping prescriptions named in `duplicate_of` but on another page of the history. */
  elsewhere: number;
}

/** "ตรวจซ้ำซ้อน": the other courses of the same drug that overlap this one. */
export function DuplicateWarning({ others, elsewhere }: Props) {
  const text = prescriptionsPage.duplicate;

  return (
    <div role="note" className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
      <p className="font-medium">{text.title}</p>
      <ul className="list-disc pl-5">
        {others.map((other) => (
          <li key={other.id}>{text.other(other.drug_name, formatDateTime(other.prescribed_at))}</li>
        ))}
        {elsewhere > 0 && <li>{text.elsewhere(elsewhere)}</li>}
      </ul>
      <p className="text-xs text-red-700">{text.note}</p>
    </div>
  );
}
