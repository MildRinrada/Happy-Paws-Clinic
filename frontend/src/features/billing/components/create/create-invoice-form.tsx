'use client';

import { useRouter } from 'next/navigation';
import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Textarea } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { errorsUnder } from '@/features/inventory/lib/errors-under';
import { useFormSubmit } from '@/hooks/use-form-submit';
import {
  createInvoiceAction,
  customerPetsAction,
  type CustomerChoice,
  type PetChoice,
} from '../../actions/create-invoice-actions';
import { billingMessages } from '../../content/billing-messages';
import { createInvoicePage } from '../../content/create-invoice-page';
import type { CatalogueEntry } from '../../lib/catalogue-entry';
import { draftSubtotal, emptyLineDraft, toLineInput, type InvoiceLineDraft } from '../../lib/invoice-line-draft';
import type { InvoiceLineInput } from '../../types/billing.types';
import { CustomerPicker } from './customer-picker';
import { DraftTotal } from './draft-total';
import { InvoiceLineRow } from './invoice-line-row';
import { PetSelect } from './pet-select';

interface Props {
  branchId: string;
  canSearchCustomers: boolean;
  catalogue: CatalogueEntry[];
}

const MAX_LINES = 100;

/**
 * Raises a bill at the counter (`POST /staff/invoices`): the customer, optionally the pet, and the
 * lines. The total shown is a preview; the bill's figures are the backend's, and the cashier lands
 * on the new bill to take the money.
 *
 * One idempotency key per bill, kept across retries so a double-click raises one bill. A refusal
 * mints a new one, because the backend stores a refusal under its key and replays it.
 */
export function CreateInvoiceForm({ branchId, canSearchCustomers, catalogue }: Props) {
  const text = createInvoicePage;
  const router = useRouter();
  const [customer, setCustomer] = useState<CustomerChoice | null>(null);
  const [pets, setPets] = useState<PetChoice[]>([]);
  const [petId, setPetId] = useState('');
  const [lines, setLines] = useState<InvoiceLineDraft[]>(() => [emptyLineDraft()]);
  const [note, setNote] = useState('');
  const [key, setKey] = useState(() => crypto.randomUUID());
  const [local, setLocal] = useState<string>();
  const [, startPets] = useTransition();
  const { errors, message, pending, saved, submit, touch } = useFormSubmit(billingMessages);

  function edit(apply: () => void) {
    touch();
    setLocal(undefined);
    apply();
  }

  function choose(next: CustomerChoice | null) {
    edit(() => {
      setCustomer(next);
      setPets([]);
      setPetId('');
    });

    if (next && canSearchCustomers) {
      startPets(async () => {
        const result = await customerPetsAction({ customerId: next.id }).catch(() => null);
        if (result?.ok) setPets(result.data);
      });
    }
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const inputs = lines.map(toLineInput);

    if (!customer || inputs.some((line) => line === null)) {
      setLocal(text.incomplete);
      return;
    }

    submit(
      async () => {
        const result = await createInvoiceAction({
          customerId: customer.id,
          branchId,
          petId: petId || undefined,
          note: note.trim() || undefined,
          lines: inputs as InvoiceLineInput[],
          idempotencyKey: key,
        });

        if (!result.ok && result.error.status < 500) {
          setKey(crypto.randomUUID());
        }

        return result;
      },
      (invoice) => router.push(`/billing/${invoice.id}`),
    );
  }

  return (
    <div className="space-y-5">
      <section className="space-y-3">
        <h2 className="text-sm font-medium text-ink-800">{text.customer.heading}</h2>
        <CustomerPicker canSearch={canSearchCustomers} chosen={customer} error={errors.customer_id} onChoose={choose} />
        {customer && pets.length > 0 && (
          <PetSelect pets={pets} value={petId} error={errors.pet_id} onChange={(value) => edit(() => setPetId(value))} />
        )}
      </section>

      <form onSubmit={save} className="space-y-4" noValidate>
        <div className="space-y-3">
          <h2 className="text-sm font-medium text-ink-800">{text.lines.heading}</h2>
          {catalogue.length > 0 && <p className="text-xs text-ink-500">{text.lines.catalogueNote}</p>}
          {errors.lines && (
            <p role="alert" className="text-sm text-red-700">
              {errors.lines}
            </p>
          )}
          {lines.map((line, index) => (
            <InvoiceLineRow
              key={line.key}
              index={index}
              line={line}
              catalogue={catalogue}
              errors={errorsUnder(errors, `lines[${index}].`)}
              onChange={(next) => edit(() => setLines((current) => current.map((candidate) => (candidate.key === line.key ? next : candidate))))}
              onRemove={
                lines.length > 1
                  ? () => edit(() => setLines((current) => current.filter((candidate) => candidate.key !== line.key)))
                  : undefined
              }
            />
          ))}
          {lines.length < MAX_LINES && (
            <Button size="sm" variant="secondary" onClick={() => edit(() => setLines((current) => [...current, emptyLineDraft()]))}>
              {text.lines.add}
            </Button>
          )}
        </div>

        <Field label={text.note} htmlFor="invoice-note" error={errors.note}>
          <Textarea id="invoice-note" rows={2} maxLength={1000} value={note} onChange={(event) => edit(() => setNote(event.target.value))} />
        </Field>

        <DraftTotal subtotalSatang={draftSubtotal(lines)} />

        <div className="flex flex-wrap items-center gap-3">
          <Button type="submit" disabled={pending || saved}>
            {pending || saved ? text.saving : text.submit}
          </Button>
          <FormStatus message={local ?? message ?? errors.branch_id ?? errors.consultation_id} savedText="" />
        </div>
      </form>
    </div>
  );
}
