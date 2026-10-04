import type { TemplateMedicine } from '@/features/prescriptions/types/treatment-template.types';
import { treatmentTemplates } from '../../content/treatment-templates';

/**
 * A template's medicines, for reading only. They are prescribed in the prescription part, which
 * runs the allergy check; this form bills procedures and cannot write a prescription.
 */
export function TemplateMedicines({ medicines }: { medicines: TemplateMedicine[] }) {
  const text = treatmentTemplates;

  if (medicines.length === 0) return null;

  return (
    <div className="space-y-1">
      <p className="text-xs font-medium text-ink-500">{text.medicines}</p>
      <ul className="space-y-0.5 text-sm text-ink-700">
        {medicines.map((medicine, index) => (
          <li key={`${medicine.drug_name}-${index}`}>
            <span className="font-medium text-ink-900">{medicine.drug_name}</span>{' '}
            {text.medicine(medicine.dose, medicine.frequency, medicine.duration_days, medicine.quantity)}
            {medicine.instructions && ` ${medicine.instructions}`}
          </li>
        ))}
      </ul>
    </div>
  );
}
