'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateMedicalRecordSettingsAction } from '../../actions/clinic-rules-actions';
import { rulesSettings } from '../../content/rules-settings';
import { parseLineList } from '../../lib/parse-list';
import type { MedicalRecordSettings } from '../../types/settings.types';

/** What a case must hold before it closes, the examination form, what owners see, and who signs. */
export function RecordRulesForm({ settings }: { settings: MedicalRecordSettings }) {
  const text = rulesSettings.record;
  const [required, setRequired] = useState(settings.required_fields);
  const [systems, setSystems] = useState(settings.examination_systems.join('\n'));
  const [visible, setVisible] = useState(settings.owner_visible_sections);
  const [caseVetSigns, setCaseVetSigns] = useState(settings.signer_must_be_case_vet);
  const [version, setVersion] = useState(settings.version);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  const changed = <T,>(set: (value: T) => void) => (value: T) => {
    touch();
    set(value);
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () =>
        updateMedicalRecordSettingsAction({
          version,
          values: {
            required_fields: required,
            examination_systems: parseLineList(systems),
            owner_visible_sections: visible,
            signer_must_be_case_vet: caseVetSigns,
          },
        }),
      (stored) => setVersion(stored.version),
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} aside={<span className="text-xs text-ink-500">{text.auditNote}</span>} />
      <CardBody>
        <form onSubmit={save} className="space-y-6">
          <CheckboxGroup
            legend={text.requiredFields}
            hint={text.requiredFieldsHint}
            error={errors.required_fields}
            options={rulesSettings.requiredFieldLabels}
            selected={required}
            onChange={changed(setRequired)}
          />

          <Field label={text.examinationSystems} htmlFor="record-systems" hint={text.examinationSystemsHint} error={errors.examination_systems}>
            <Textarea id="record-systems" rows={6} value={systems} className="max-w-md" onChange={(event) => changed(setSystems)(event.target.value)} />
          </Field>

          <CheckboxGroup
            legend={text.ownerVisibleSections}
            error={errors.owner_visible_sections}
            options={rulesSettings.sectionLabels}
            selected={visible}
            onChange={changed(setVisible)}
          />

          <Checkbox label={text.signerMustBeCaseVet} checked={caseVetSigns} onChange={(event) => changed(setCaseVetSigns)(event.target.checked)} />

          <div className="flex flex-wrap items-center justify-end gap-3">
            <FormStatus message={message} saved={saved} savedText={formMessages.saved} />
            <Button type="submit" disabled={pending}>
              {pending ? formMessages.saving : formMessages.save}
            </Button>
          </div>
        </form>
      </CardBody>
    </Card>
  );
}
