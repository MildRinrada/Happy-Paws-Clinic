'use client';

import { useState, type FormEvent } from 'react';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updateClinicProfileAction } from '../../actions/clinic-setup-actions';
import { clinicSettings } from '../../content/clinic-settings';
import type { ClinicProfile } from '../../types/clinic.types';
import type { ClinicProfileInput } from '../../types/settings.types';
import { ClinicDetailsCard } from './clinic-details-card';
import { ClinicLogoCard } from './clinic-logo-card';

const FORM_ID = 'clinic-profile-form';

const toInput = (profile: ClinicProfile): ClinicProfileInput => ({
  name: profile.name,
  logo_url: profile.logo_url ?? '',
  // Not on the form any more (nothing on the website uses it), but sent back as stored so a save
  // here never clears it.
  brand_color: profile.brand_color ?? '',
  license_no: profile.license_no ?? '',
  operator_name: profile.operator_name ?? '',
  tab_icon_url: profile.tab_icon_url ?? '',
  show_name_with_logo: profile.show_name_with_logo ?? true,
});

/**
 * The clinic's name, licence, operator, logo and tab icon: two cards, one form, saved together by
 * the settings save bar. Every field is sent; an empty one is cleared. The logo, the switch for the
 * name beside it and the tab icon (docs/contract-extensions.md § 50) are saved with the rest, so a
 * picture chosen and then abandoned never reaches the website.
 */
export function ProfileForm({ profile }: { profile: ClinicProfile }) {
  const [values, setValues] = useState(() => toInput(profile));
  const [version, setVersion] = useState(profile.version);
  // Cancel also forgets the logo section's notes about a file just picked ("อัปโหลดแล้ว ...").
  const [resets, setResets] = useState(0);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: clinicSettings.profile.section,
    value: values,
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setValues(stored);
      setResets((count) => count + 1);
    },
  });

  const set = <K extends keyof ClinicProfileInput>(field: K, value: ClinicProfileInput[K]) => {
    touch();
    setValues((current) => ({ ...current, [field]: value }));
  };

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sent = values;

    submit(
      () => updateClinicProfileAction({ version, values: sent }),
      (stored) => {
        setVersion(stored.version);
        changes.markSaved(sent);
      },
    );
  }

  return (
    <form id={FORM_ID} onSubmit={save} className="space-y-6">
      <ClinicDetailsCard values={values} errors={errors} onChange={set} />
      <ClinicLogoCard key={resets} values={values} errors={errors} onChange={set} />
      <SectionFooter pending={pending} message={message} saved={saved} inBar={changes.inBar} />
    </form>
  );
}
