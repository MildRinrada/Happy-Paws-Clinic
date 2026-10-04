'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { createLabRequestAction } from '../../actions/lab-actions';
import { labOrder } from '../../content/lab-order';
import { labPage } from '../../content/lab-page';
import { errorFor, otherErrors } from '../../lib/field-error';
import { emptyTest, testsFrom, type TestDraft } from '../../lib/test-drafts';
import type { LabDestination } from '../../types/lab.types';
import { TestRowList } from './test-row-list';

interface LabOrderFormProps {
  petId: string;
  consultationId: string;
}

const SHOWN = ['tests', 'destination', 'external_lab_name', 'clinical_notes'] as const;

/**
 * Sends a sample for testing from the case. The request carries the case id, so the branch is the
 * case's and the request shows on this page; it is still its own record, so results can arrive
 * after the case is signed.
 */
export function LabOrderForm({ petId, consultationId }: LabOrderFormProps) {
  const text = labOrder;
  const [tests, setTests] = useState<TestDraft[]>(() => [emptyTest()]);
  const [destination, setDestination] = useState<LabDestination>('in_house');
  const [externalName, setExternalName] = useState('');
  const [urgent, setUrgent] = useState(false);
  const [notes, setNotes] = useState('');
  const [testsError, setTestsError] = useState<string>();
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(labPage.messages);

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const chosen = testsFrom(tests);
    setTestsError(chosen.length === 0 ? text.noTests : undefined);
    if (chosen.length === 0) return;

    submit(
      () =>
        createLabRequestAction({
          pet_id: petId,
          consultation_id: consultationId,
          destination,
          external_lab_name: externalName,
          priority: urgent ? 'urgent' : 'routine',
          clinical_notes: notes,
          tests: chosen,
        }),
      () => {
        setTests([emptyTest()]);
        setDestination('in_house');
        setExternalName('');
        setUrgent(false);
        setNotes('');
      },
    );
  }

  return (
    <form onSubmit={save} onChange={touch} className="space-y-3 border-t border-ink-100 pt-4">
      <fieldset className="space-y-2">
        <legend className="text-sm font-medium text-ink-700">{text.tests}</legend>
        <TestRowList drafts={tests} onChange={setTests} />
        {(testsError ?? errorFor(errors, 'tests')) && (
          <p role="alert" className="text-sm text-red-700">
            {testsError ?? errorFor(errors, 'tests')}
          </p>
        )}
      </fieldset>

      <fieldset className="space-y-2">
        <legend className="text-sm font-medium text-ink-700">{text.destination}</legend>
        <div className="flex flex-wrap gap-4 text-sm text-ink-700">
          {(['in_house', 'external'] as const).map((value) => (
            <label key={value} className="flex items-center gap-2">
              <input
                type="radio"
                name={`lab-destination-${consultationId}`}
                value={value}
                checked={destination === value}
                onChange={() => setDestination(value)}
                className="size-4 accent-brand-600"
              />
              {value === 'in_house' ? text.inHouse : text.external}
            </label>
          ))}
          <label className="flex items-center gap-2">
            <input type="checkbox" checked={urgent} onChange={(event) => setUrgent(event.target.checked)} className="size-4 accent-brand-600" />
            {text.urgent}
          </label>
        </div>
        {errors.destination && <p role="alert" className="text-sm text-red-700">{errors.destination}</p>}
      </fieldset>

      {destination === 'external' && (
        <Field label={text.externalName} htmlFor="lab-external-name" error={errors.external_lab_name}>
          <Input id="lab-external-name" value={externalName} maxLength={200} required onChange={(event) => setExternalName(event.target.value)} />
        </Field>
      )}

      <Field label={text.notes} htmlFor="lab-notes" error={errors.clinical_notes}>
        <Textarea id="lab-notes" rows={2} value={notes} maxLength={2000} placeholder={text.notesPlaceholder} onChange={(event) => setNotes(event.target.value)} />
      </Field>

      <div className="flex flex-wrap items-center justify-end gap-3">
        <FormStatus message={message ?? otherErrors(errors, SHOWN)[0]} saved={saved} savedText={text.sent} />
        <Button type="submit" disabled={pending}>
          {pending ? text.working : text.submit}
        </Button>
      </div>
    </form>
  );
}
