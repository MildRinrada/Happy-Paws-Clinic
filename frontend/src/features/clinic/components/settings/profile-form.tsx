'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateClinicProfileAction } from '../../actions/clinic-setup-actions';
import { clinicSettings } from '../../content/clinic-settings';
import type { ClinicProfile } from '../../types/clinic.types';
import type { ClinicProfileInput } from '../../types/settings.types';

const toInput = (profile: ClinicProfile): ClinicProfileInput => ({
  name: profile.name,
  logo_url: profile.logo_url ?? '',
  brand_color: profile.brand_color ?? '',
  license_no: profile.license_no ?? '',
  operator_name: profile.operator_name ?? '',
});

/** The clinic's name, branding and licence. Every field is sent; an empty one is cleared. */
export function ProfileForm({ profile }: { profile: ClinicProfile }) {
  const text = clinicSettings.profile;
  const [values, setValues] = useState(() => toInput(profile));
  const [version, setVersion] = useState(profile.version);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);

  const set = (field: keyof ClinicProfileInput, value: string) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(() => updateClinicProfileAction({ version, values }), (stored) => setVersion(stored.version));
  }

  const swatch = /^#[0-9a-f]{6}$/i.test(values.brand_color) ? values.brand_color : '#ffffff';

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody>
        <form onSubmit={save} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={text.name} htmlFor="clinic-name" error={errors.name}>
              <Input id="clinic-name" value={values.name} maxLength={200} required onChange={(event) => set('name', event.target.value)} />
            </Field>

            <Field label={text.brandColor} htmlFor="clinic-color" hint={text.brandColorHint} error={errors.brand_color}>
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  aria-label={text.brandColor}
                  value={swatch}
                  className="h-10 w-12 shrink-0 cursor-pointer rounded-lg border border-ink-200 bg-white p-1"
                  onChange={(event) => set('brand_color', event.target.value.toUpperCase())}
                />
                <Input id="clinic-color" value={values.brand_color} maxLength={7} className="max-w-32 font-mono" onChange={(event) => set('brand_color', event.target.value)} />
              </div>
            </Field>

            <div className="sm:col-span-2">
              <Field label={text.logoUrl} htmlFor="clinic-logo" hint={text.logoUrlHint} error={errors.logo_url}>
                <Input id="clinic-logo" type="url" value={values.logo_url} maxLength={1000} placeholder="https://" onChange={(event) => set('logo_url', event.target.value)} />
              </Field>
            </div>

            <Field label={text.licenseNo} htmlFor="clinic-license" error={errors.license_no}>
              <Input id="clinic-license" value={values.license_no} maxLength={64} onChange={(event) => set('license_no', event.target.value)} />
            </Field>

            <Field label={text.operatorName} htmlFor="clinic-operator" hint={text.operatorNameHint} error={errors.operator_name}>
              <Input id="clinic-operator" value={values.operator_name} maxLength={200} onChange={(event) => set('operator_name', event.target.value)} />
            </Field>
          </div>

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
