import type { TemplateProcedure, TreatmentTemplate } from '@/features/prescriptions/types/treatment-template.types';
import { treatmentTemplates } from '../../content/treatment-templates';
import { TemplateMedicines } from './template-medicines';
import { TemplateProcedures } from './template-procedures';

interface Props {
  template: TreatmentTemplate;
  onUse: (procedure: TemplateProcedure) => void;
}

/** One template opened: what it is for, its procedures to copy, and its medicines and plan to read. */
export function TemplateDetails({ template, onUse }: Props) {
  const text = treatmentTemplates;
  const course = template.sessions ? text.course(template.sessions, template.session_interval_days) : undefined;
  const summary = [template.description, course].filter(Boolean).join(' ');

  return (
    <div className="space-y-3">
      {summary && <p className="text-sm text-ink-600">{summary}</p>}
      <TemplateProcedures procedures={template.procedures} onUse={onUse} />
      <TemplateMedicines medicines={template.medicines} />
      {template.plan && (
        <p className="text-sm text-ink-700">
          <span className="font-medium text-ink-500">{text.plan}:</span> {template.plan}
        </p>
      )}
    </div>
  );
}
