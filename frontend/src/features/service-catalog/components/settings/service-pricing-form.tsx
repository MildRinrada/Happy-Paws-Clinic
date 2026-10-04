'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Field, Input } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateServicePricingAction } from '../../actions/service-actions';
import { serviceSettings } from '../../content/service-settings';
import { inputToSatang, satangToInput } from '../../lib/baht-input';
import { tierRow, tiersPayload, type TierRow } from '../../lib/tier-rows';
import type { ManagedService } from '../../types/managed-service.types';
import { PriceTierRow } from './price-tier-row';

interface ServicePricingFormProps {
  service: ManagedService;
  /** Today in clinic time: the earliest day a price list can start. */
  today: string;
  onDone: () => void;
}

/**
 * A dated price list. It starts from the list in force today; saving it for a later day leaves
 * today's price alone until then.
 */
export function ServicePricingForm({ service, today, onDone }: ServicePricingFormProps) {
  const text = serviceSettings.pricing;
  const [base, setBase] = useState(satangToInput(service.pricing.base_price_satang));
  const [effectiveFrom, setEffectiveFrom] = useState(today);
  const [rows, setRows] = useState<TierRow[]>(() => service.pricing.tiers.map((tier) => tierRow(tier)));
  const [local, setLocal] = useState<{ base?: string; row?: number }>({});
  const { errors, message, pending, submit } = useFormSubmit(formMessages);
  const id = (field: string) => `pricing-${service.id}-${field}`;

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setLocal({});

    const basePrice = inputToSatang(base);
    if (basePrice === null) return setLocal({ base: text.badPrice });

    const tiers = tiersPayload(rows);
    if ('badRow' in tiers) return setLocal({ row: tiers.badRow });

    submit(
      () =>
        updateServicePricingAction({
          id: service.id,
          version: service.version,
          values: { base_price_satang: basePrice, effective_from: effectiveFrom, tiers: tiers.tiers },
        }),
      onDone,
    );
  }

  // The backend names a bad tier `tiers[1].max_weight_kg`; the message goes under that row.
  const rowError = (index: number) =>
    local.row === index ? text.badPrice : Object.entries(errors).find(([field]) => field.startsWith(`tiers[${index}]`))?.[1];

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={text.base} htmlFor={id('base')} error={local.base ?? errors.base_price_satang}>
          <Input id={id('base')} inputMode="decimal" value={base} required className="max-w-40" onChange={(event) => setBase(event.target.value)} />
        </Field>

        <Field label={text.effectiveFrom} htmlFor={id('from')} hint={text.effectiveFromHint} error={errors.effective_from}>
          <Input id={id('from')} type="date" min={today} value={effectiveFrom} required className="max-w-44" onChange={(event) => setEffectiveFrom(event.target.value)} />
        </Field>
      </div>

      <fieldset className="space-y-2">
        <legend className="text-sm font-medium text-ink-700">{text.tiers}</legend>
        <p className="text-sm text-ink-500">{text.tiersHint}</p>
        {errors.tiers && (
          <p role="alert" className="text-sm text-red-700">
            {errors.tiers}
          </p>
        )}
        <ul className="space-y-2">
          {rows.map((row, index) => (
            <PriceTierRow
              key={row.key}
              row={row}
              error={rowError(index)}
              onChange={(next) => setRows((current) => current.map((entry) => (entry.key === row.key ? next : entry)))}
              onRemove={() => setRows((current) => current.filter((entry) => entry.key !== row.key))}
            />
          ))}
        </ul>
        <Button variant="secondary" size="sm" onClick={() => setRows((current) => [...current, tierRow()])}>
          {text.addTier}
        </Button>
      </fieldset>

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}
