'use client';

import { useState, useTransition, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { customerPetsAction, type CustomerChoice, type PetChoice } from '@/features/billing/actions/create-invoice-actions';
import { CustomerPicker } from '@/features/billing/components/create/customer-picker';
import { PetSelect } from '@/features/billing/components/create/pet-select';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { sellPackageAction } from '../../actions/crm-actions';
import { crmArea } from '../../content/crm-tabs';
import { packageSalePage } from '../../content/package-sale-page';
import { emptyPackageDraft, packageProblems, toSaleInput, type PackageDraft } from '../../lib/package-draft';
import type { PackageSale, ServiceOption } from '../../types/crm.types';
import { ServiceChecklist } from '../shared/service-checklist';
import { PackageSaleResult } from './package-sale-result';
import { PackageTermsFields } from './package-terms-fields';

interface Props {
  branchId: string;
  canSearchCustomers: boolean;
  canReadLoyalty: boolean;
  services: ServiceOption[];
  today: string;
}

/**
 * Sells a package (`POST /staff/packages`): the customer, optionally the pet, the services it
 * covers, how many uses and the price. The bill comes back with it, unpaid.
 *
 * The idempotency key works as on `/billing/new`: one per attempt, kept across a retry of the same
 * form so a double-click sells one package. Changing the form, or a refusal, mints a new one,
 * because the backend stores a refusal under its key and replays it.
 */
export function PackageSaleForm({ branchId, canSearchCustomers, canReadLoyalty, services, today }: Props) {
  const text = packageSalePage;
  const [customer, setCustomer] = useState<CustomerChoice | null>(null);
  const [pets, setPets] = useState<PetChoice[]>([]);
  const [petId, setPetId] = useState('');
  const [draft, setDraft] = useState<PackageDraft>(emptyPackageDraft);
  const [key, setKey] = useState(() => crypto.randomUUID());
  const [tried, setTried] = useState(false);
  const [sale, setSale] = useState<PackageSale>();
  const [, startPets] = useTransition();
  const { errors, message, pending, submit, touch } = useFormSubmit({ conflict: crmArea.conflict, unreachable: crmArea.unreachable });
  const problems = packageProblems(draft);
  const show = (problem: (typeof problems)[number]) => (tried && problems.includes(problem) ? text.errors[problem] : undefined);

  function edit(apply: () => void) {
    touch();
    setKey(crypto.randomUUID());
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
    setTried(true);

    if (!customer || problems.length > 0) {
      return;
    }

    const input = toSaleInput(draft, { customerId: customer.id, petId, branchId });

    submit(async () => {
      const result = await sellPackageAction({ idempotencyKey: key, input });
      if (!result.ok && result.error.status < 500) setKey(crypto.randomUUID());
      return result;
    }, setSale);
  }

  function another() {
    setSale(undefined);
    setCustomer(null);
    setPets([]);
    setPetId('');
    setDraft(emptyPackageDraft());
    setTried(false);
    setKey(crypto.randomUUID());
  }

  if (sale) {
    return <PackageSaleResult sale={sale} canReadLoyalty={canReadLoyalty} onAnother={another} />;
  }

  return (
    <div className="space-y-5">
      <section className="space-y-3">
        <h2 className="text-sm font-medium text-ink-800">{text.customerHeading}</h2>
        <CustomerPicker
          canSearch={canSearchCustomers}
          chosen={customer}
          error={errors.customer_id ?? (tried && !customer ? text.errors.customer : undefined)}
          onChoose={choose}
        />
        {customer && pets.length > 0 && (
          <div className="space-y-1">
            <PetSelect pets={pets} value={petId} error={errors.pet_id} onChange={(value) => edit(() => setPetId(value))} />
            <p className="text-xs text-ink-500">{text.petHint}</p>
          </div>
        )}
      </section>

      <form onSubmit={save} className="space-y-5" noValidate>
        <ServiceChecklist
          legend={text.services}
          hint={text.servicesHint}
          emptyText={text.noServices}
          error={errors.service_ids ?? show('services')}
          services={services}
          selected={draft.serviceIds}
          onChange={(serviceIds) => edit(() => setDraft((current) => ({ ...current, serviceIds })))}
        />

        <PackageTermsFields
          draft={draft}
          today={today}
          errors={{
            name: errors.name ?? show('name'),
            totalUses: errors.total_uses ?? show('totalUses'),
            price: errors.price_satang ?? show('price'),
            expiresOn: errors.expires_on,
            note: errors.note,
          }}
          onChange={(patch) => edit(() => setDraft((current) => ({ ...current, ...patch })))}
        />

        <div className="flex flex-wrap items-center gap-3">
          <Button type="submit" disabled={pending}>
            {pending ? text.saving : text.submit}
          </Button>
          <FormStatus message={message ?? errors.branch_id} savedText="" />
        </div>
      </form>
    </div>
  );
}
