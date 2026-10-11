'use client';

import { useState, type FormEvent } from 'react';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { CheckboxGroup } from '@/components/ui/checkbox-group';
import { Field, Textarea } from '@/components/ui/field';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateMedicalRecordSettingsAction } from '../../actions/clinic-rules-actions';
import { rulesSettings } from '../../content/rules-settings';
import { parseLineList } from '../../lib/parse-list';
import type { MedicalRecordSettings } from '../../types/settings.types';

const FORM_ID = 'record-rules-form';

/**
 * What a case must hold before it closes, the examination form, what owners see, and who signs.
 * Saved by the save bar.
 */
export function RecordRulesForm({ settings }: { settings: MedicalRecordSettings }) {
  const text = rulesSettings.record;
  const [required, setRequired] = useState(settings.required_fields);
  const [systems, setSystems] = useState(settings.examination_systems.join('\n'));
  const [visible, setVisible] = useState(settings.owner_visible_sections);
  const [caseVetSigns, setCaseVetSigns] = useState(settings.signer_must_be_case_vet);
  const [version, setVersion] = useState(settings.version);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: { required, systems, visible, caseVetSigns },
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setRequired(stored.required);
      setSystems(stored.systems);
      setVisible(stored.visible);
      setCaseVetSigns(stored.caseVetSigns);
    },
  });

  const changed = <T,>(set: (value: T) => void) => (value: T) => {
    touch();
    set(value);
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sent = { required, systems, visible, caseVetSigns };

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
      (stored) => {
        setVersion(stored.version);
        changes.markSaved(sent);
      },
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} aside={<span className="text-xs text-ink-500">{text.auditNote}</span>} />
      <CardBody>
        <form id={FORM_ID} onSubmit={save} className="space-y-6">
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

          <SectionFooter pending={pending} message={message} saved={saved} inBar={changes.inBar} />
        </form>
      </CardBody>
    </Card>
  );
}
