'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input, Select, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { generateDocumentAction } from '../../actions/document-actions';
import { petDocuments } from '../../content/pet-documents';
import { petForms } from '../../content/pet-forms';
import { PRINTABLE_DOCUMENTS } from '../../lib/document-rules';
import { savePdf } from '../../lib/save-pdf';
import type { GeneratedDocumentType } from '../../types/pet.types';

/**
 * Prints a vaccination book, a health certificate or a referral letter from the record, as a PDF
 * download. A referral says where to and why; a certificate may carry its own wording. Each print
 * is written to the access log by the backend.
 */
export function PrintDocumentForm({ petId }: { petId: string }) {
  const text = petDocuments.print;
  const [type, setType] = useState<GeneratedDocumentType>('vaccination_book');
  const [referTo, setReferTo] = useState('');
  const [reason, setReason] = useState('');
  const [remarks, setRemarks] = useState('');
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(petForms);

  function print(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => generateDocumentAction({ petId, type, referTo, reason, remarks }),
      (pdf) => savePdf(pdf.fileName, pdf.base64),
    );
  }

  return (
    <form onSubmit={print} className="space-y-3">
      <h3 className="text-xs font-semibold text-ink-500">{text.heading}</h3>

      <Field label={text.type} htmlFor="print-type" hint={type === 'vaccination_book' ? undefined : text.signerHint} error={errors.type}>
        <Select id="print-type" value={type} onChange={(event) => { touch(); setType(event.target.value as GeneratedDocumentType); }}>
          {PRINTABLE_DOCUMENTS.map((key) => (
            <option key={key} value={key}>
              {text.types[key]}
            </option>
          ))}
        </Select>
      </Field>

      {type === 'referral_letter' && (
        <>
          <Field label={text.referTo} htmlFor="print-refer-to" error={errors.refer_to}>
            <Input
              id="print-refer-to"
              value={referTo}
              placeholder={text.referToPlaceholder}
              maxLength={200}
              required
              aria-invalid={Boolean(errors.refer_to)}
              onChange={(event) => { touch(); setReferTo(event.target.value); }}
            />
          </Field>
          <Field label={text.reason} htmlFor="print-reason" error={errors.reason ?? errors.consultation_id}>
            <Textarea
              id="print-reason"
              value={reason}
              maxLength={1000}
              rows={2}
              required
              aria-invalid={Boolean(errors.reason ?? errors.consultation_id)}
              onChange={(event) => { touch(); setReason(event.target.value); }}
            />
          </Field>
        </>
      )}

      {type === 'health_certificate' && (
        <Field label={text.remarks} htmlFor="print-remarks" hint={text.remarksHint} error={errors.remarks}>
          <Textarea id="print-remarks" value={remarks} maxLength={1000} rows={2} onChange={(event) => { touch(); setRemarks(event.target.value); }} />
        </Field>
      )}

      <div className="flex flex-wrap items-center justify-end gap-2">
        <FormStatus message={message} saved={saved} savedText={text.done} />
        <Button type="submit" variant="secondary" size="sm" disabled={pending}>
          {pending ? text.working : text.submit}
        </Button>
      </div>
    </form>
  );
}
