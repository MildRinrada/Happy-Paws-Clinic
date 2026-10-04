import { prescriptionsPage } from '../content/prescriptions-page';
import type { PrescriptionItem } from '../types/prescription.types';
import { PrescriptionRow } from './prescription-row';

interface Props {
  /** Already ordered: still to hand over first. */
  items: PrescriptionItem[];
  canDispense: boolean;
  canOpenCase: boolean;
}

/**
 * A patient's medicines. One list rather than two sections, so a row keeps its key, and the label
 * it is showing, when a dispense moves it down among the handed-over ones.
 */
export function PrescriptionHistory({ items, canDispense, canOpenCase }: Props) {
  const text = prescriptionsPage;

  if (items.length === 0) return <p className="text-sm text-ink-500">{text.pet.empty}</p>;

  const byId = new Map(items.map((item) => [item.id, item]));

  return (
    <div className="space-y-2">
      <p className="no-print text-sm text-ink-500">
        {text.pet.hint}
        {!canDispense && ` ${text.dispense.noScope}`}
      </p>
      <ul className="divide-y divide-ink-100 print:divide-y-0">
        {items.map((item) => (
          <PrescriptionRow key={item.id} item={item} byId={byId} canDispense={canDispense} canOpenCase={canOpenCase} />
        ))}
      </ul>
    </div>
  );
}
