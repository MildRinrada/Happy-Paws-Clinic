'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Select } from '@/components/ui/field';
import { listTreatmentTemplatesAction } from '@/features/prescriptions/actions/template-actions';
import type { TemplateProcedure, TreatmentTemplate } from '@/features/prescriptions/types/treatment-template.types';
import { treatmentTemplates } from '../../content/treatment-templates';
import { TemplateDetails } from './template-details';

/**
 * "เทมเพลตแผนรักษา" on the procedures form. The list is read when the picker is first opened, not
 * with the page. Using a procedure only fills the form: nothing is saved until the vet presses add,
 * and the price is theirs to enter, since a template holds none.
 */
export function TemplatePicker({ onUse }: { onUse: (procedure: TemplateProcedure) => void }) {
  const text = treatmentTemplates;
  const [open, setOpen] = useState(false);
  const [templates, setTemplates] = useState<TreatmentTemplate[]>();
  const [chosenId, setChosenId] = useState('');
  const [error, setError] = useState<string>();
  const [loading, startTransition] = useTransition();

  function load() {
    setError(undefined);
    startTransition(async () => {
      try {
        const result = await listTreatmentTemplatesAction();
        if (result.ok) setTemplates(result.data);
        else setError(result.error.message);
      } catch {
        setError(text.unreachable);
      }
    });
  }

  function show() {
    setOpen(true);
    if (!templates) load();
  }

  if (!open) {
    return (
      <Button size="sm" variant="secondary" onClick={show}>
        {text.open}
      </Button>
    );
  }

  const chosen = templates?.find((template) => template.id === chosenId);

  return (
    <div className="space-y-3 rounded-lg border border-ink-200 bg-ink-50 p-3">
      {loading && <p className="text-sm text-ink-500">{text.loading}</p>}

      {error && (
        <p role="alert" className="flex flex-wrap items-center gap-2 text-sm text-red-700">
          {error}
          <Button size="sm" variant="ghost" onClick={load}>
            {text.retry}
          </Button>
        </p>
      )}

      {templates?.length === 0 && <p className="text-sm text-ink-500">{text.none}</p>}

      {templates && templates.length > 0 && (
        <Field label={text.label} htmlFor="treatment-template">
          <Select id="treatment-template" value={chosenId} onChange={(event) => setChosenId(event.target.value)}>
            <option value="">{text.choose}</option>
            {templates.map((template) => (
              <option key={template.id} value={template.id}>
                {text.option(template.name, template.species ?? text.anySpecies)}
              </option>
            ))}
          </Select>
        </Field>
      )}

      {chosen && <TemplateDetails template={chosen} onUse={onUse} />}

      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="text-xs text-ink-500">{text.hint}</p>
        <Button size="sm" variant="ghost" onClick={() => setOpen(false)}>
          {text.close}
        </Button>
      </div>
    </div>
  );
}
