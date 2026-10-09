'use client';

import { Field, Input, Textarea } from '@/components/ui/field';
import { packageSalePage } from '../../content/package-sale-page';
import type { PackageDraft } from '../../lib/package-draft';

interface Props {
  draft: PackageDraft;
  /** Today in clinic time: an expiry cannot be in the past. */
  today: string;
  errors: Partial<Record<'name' | 'totalUses' | 'price' | 'expiresOn' | 'note', string>>;
  onChange: (patch: Partial<PackageDraft>) => void;
}

/** What is sold: the package's name, how many uses, the price, and until when. */
export function PackageTermsFields({ draft, today, errors, onChange }: Props) {
  const text = packageSalePage;

  return (
    <div className="space-y-4">
      <Field label={text.name} htmlFor="package-name" error={errors.name}>
        <Input
          id="package-name"
          value={draft.name}
          maxLength={200}
          placeholder={text.namePlaceholder}
          onChange={(event) => onChange({ name: event.target.value })}
        />
      </Field>

      <div className="grid gap-3 sm:grid-cols-3">
        <Field label={text.totalUses} htmlFor="package-uses" error={errors.totalUses}>
          <Input
            id="package-uses"
            type="number"
            min={1}
            max={1000}
            step={1}
            value={draft.totalUses}
            onChange={(event) => onChange({ totalUses: event.target.value })}
          />
        </Field>
        <Field label={text.price} htmlFor="package-price" error={errors.price}>
          <Input id="package-price" inputMode="decimal" value={draft.price} onChange={(event) => onChange({ price: event.target.value })} />
        </Field>
        <Field label={text.expiresOn} htmlFor="package-expires" error={errors.expiresOn} hint={text.expiresHint}>
          <Input
            id="package-expires"
            type="date"
            min={today}
            value={draft.expiresOn}
            onChange={(event) => onChange({ expiresOn: event.target.value })}
          />
        </Field>
      </div>

      <Field label={text.note} htmlFor="package-note" error={errors.note}>
        <Textarea id="package-note" rows={2} maxLength={1000} value={draft.note} onChange={(event) => onChange({ note: event.target.value })} />
      </Field>
    </div>
  );
}
