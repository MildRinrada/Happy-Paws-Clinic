import Link from 'next/link';
import { consultationPaths } from '@/features/consultations/lib/consultation-paths';
import { formatDateTime } from '@/lib/format/datetime';
import { prescriptionsPage } from '../content/prescriptions-page';
import type { PrescriptionItem } from '../types/prescription.types';
import { DispenseControl } from './dispense-control';
import { DuplicateWarning } from './duplicate-warning';
import { PrescriptionStatus } from './prescription-status';

interface Props {
  item: PrescriptionItem;
  /** The rest of the page, to name the prescriptions `duplicate_of` points at. */
  byId: Map<string, PrescriptionItem>;
  canDispense: boolean;
  canOpenCase: boolean;
}

/** One prescription in the history. Only a label shown under it prints. */
export function PrescriptionRow({ item, byId, canDispense, canOpenCase }: Props) {
  const text = prescriptionsPage.item;
  const others = item.duplicate_of.flatMap((id) => byId.get(id) ?? []);
  const elsewhere = item.duplicate_of.length - others.length;

  return (
    <li className="space-y-2 py-3 print:border-0 print:py-0">
      <div className="no-print space-y-0.5 text-sm">
        <p className="flex flex-wrap items-center gap-2">
          <span className="font-medium text-ink-900">{item.drug_name}</span>
          <PrescriptionStatus item={item} />
        </p>
        <p className="text-ink-700">{text.line(item.dose, item.frequency, item.duration_days, item.quantity)}</p>
        {item.instructions && <p className="text-ink-600">{item.instructions}</p>}
        <p className="text-ink-500">
          {text.prescribedAt(formatDateTime(item.prescribed_at))} {text.courseEnds(formatDateTime(item.course_ends_at))}
          {item.dispensed_at && ` ${text.dispensedAt(formatDateTime(item.dispensed_at))}`}
          {canOpenCase && (
            <>
              {' '}
              <Link href={consultationPaths.detail(item.consultation_id)} className="text-brand-700 hover:underline">
                {text.consultation}
              </Link>
            </>
          )}
        </p>
        {item.allergy_acknowledgement && (
          <p className="text-amber-800">{text.acknowledged(item.allergy_acknowledgement)}</p>
        )}
      </div>

      {item.duplicate_of.length > 0 && (
        <div className="no-print">
          <DuplicateWarning others={others} elsewhere={elsewhere} />
        </div>
      )}

      <DispenseControl
        prescriptionId={item.id}
        dispensed={Boolean(item.dispensed_at)}
        canDispense={canDispense}
        duplicate={item.duplicate_of.length > 0}
      />
    </li>
  );
}
