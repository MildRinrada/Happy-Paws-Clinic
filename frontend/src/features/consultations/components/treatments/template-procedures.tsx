import { Button } from '@/components/ui/button';
import type { TemplateProcedure } from '@/features/prescriptions/types/treatment-template.types';
import { treatmentTemplates } from '../../content/treatment-templates';

interface Props {
  procedures: TemplateProcedure[];
  onUse: (procedure: TemplateProcedure) => void;
}

/** A template's procedures, each one a button that copies it into the form. The form adds one line at a time. */
export function TemplateProcedures({ procedures, onUse }: Props) {
  const text = treatmentTemplates;

  return (
    <div className="space-y-1">
      <p className="text-xs font-medium text-ink-500">{text.procedures}</p>
      <ul className="divide-y divide-ink-100 text-sm">
        {procedures.map((procedure, index) => (
          <li key={`${procedure.name}-${index}`} className="flex items-center justify-between gap-3 py-1.5">
            <span className="min-w-0 text-ink-900">
              {procedure.name} <span className="text-ink-500">{text.quantity(procedure.quantity)}</span>
              {procedure.note && <span className="block text-ink-600">{procedure.note}</span>}
            </span>
            <Button size="sm" variant="secondary" onClick={() => onUse(procedure)}>
              {text.use}
            </Button>
          </li>
        ))}
      </ul>
    </div>
  );
}
