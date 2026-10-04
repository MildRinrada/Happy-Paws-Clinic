import { prescriptionsPage } from '../content/prescriptions-page';
import { formatLabelDate } from '../lib/label-date';
import type { DispenseLabel } from '../types/prescription.types';

/**
 * The label stuck to the bag, laid out from the backend's lines. Black on white with a fixed width,
 * so it prints the same on a sheet or a label printer. Everything around it is `no-print`. The
 * print width is fixed because the console's column collapses once its sidebar is hidden.
 */
export function DrugLabel({ label }: { label: DispenseLabel }) {
  const text = prescriptionsPage.label;

  return (
    <article className="w-full max-w-sm rounded-md border border-dashed border-ink-400 bg-white p-4 text-sm text-black print:w-[90mm] print:max-w-none print:border-solid print:border-black">
      <header className="border-b border-ink-300 pb-2 text-center">
        <p className="font-semibold">{label.clinic_name}</p>
        <p className="text-xs">
          {label.branch_name}
          {label.clinic_license_no && ` ${text.license(label.clinic_license_no)}`}
        </p>
      </header>

      <div className="flex justify-between gap-3 border-b border-ink-300 py-2">
        <div className="min-w-0">
          <p className="font-semibold">{label.pet_name}</p>
          <p className="text-xs">
            {text.owner} {label.owner_name}
          </p>
        </div>
        <p className="shrink-0 font-mono text-xs">
          {text.hn} {label.hospital_number}
        </p>
      </div>

      <div className="space-y-0.5 py-2">
        <p className="text-base font-bold">{label.drug_name}</p>
        <p className="font-medium">{text.howTo(label.dose, label.frequency)}</p>
        <p>{text.course(label.duration_days, label.quantity)}</p>
        {label.instructions && <p>{label.instructions}</p>}
      </div>

      <footer className="flex justify-between gap-3 border-t border-ink-300 pt-2 text-xs">
        <p className="min-w-0">
          {text.prescribedBy} {label.prescribed_by}
          {label.prescriber_license_no && ` (${label.prescriber_license_no})`}
        </p>
        <p className="shrink-0">
          {text.dispensedOn} {formatLabelDate(label.dispensed_on)}
        </p>
      </footer>
      <p className="pt-1 text-right font-mono text-[10px] text-ink-500">{label.prescription_id}</p>
    </article>
  );
}
