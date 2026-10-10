'use client';

import { useId, useState, type FormEvent } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input, Textarea } from '@/components/ui/field';
import { FormFooter } from '@/features/settings/components/form-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replacePublicProfileAction } from '../../actions/public-profile-actions';
import { publicProfileText } from '../../content/public-profile';
import type { StaffMember } from '../../types/staff.types';
import { ProfilePhotoField } from './profile-photo-field';

const textFields = [
  { key: 'job_title', label: publicProfileText.jobTitle, hint: publicProfileText.jobTitleHint, max: 120 },
  { key: 'specialty', label: publicProfileText.specialty, hint: publicProfileText.specialtyHint, max: 120 },
] as const;

/**
 * What the public team page says about one person. `PUT` replaces the whole profile, so every
 * field is sent each time; a refusal (`invalid_public_profile`) is the backend's own sentence.
 */
export function PublicProfileForm({ member, onDone }: { member: StaffMember; onDone: () => void }) {
  const text = publicProfileText;
  const id = useId();
  const current = member.public_profile;
  const [draft, setDraft] = useState({
    job_title: current?.job_title ?? '',
    specialty: current?.specialty ?? '',
    bio: current?.bio ?? '',
    photo_path: current?.photo_path ?? '',
    career_start_year: current?.career_start_year?.toString() ?? '',
  });
  const [show, setShow] = useState(current?.show_on_website ?? false);
  const { errors, message, pending, submit } = useFormSubmit(formMessages);

  const set = (field: keyof typeof draft, value: string) => setDraft((values) => ({ ...values, [field]: value }));

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const year = draft.career_start_year.trim();

    submit(
      () =>
        replacePublicProfileAction({
          id: member.id,
          version: member.version,
          values: { ...draft, career_start_year: year ? Number(year) : undefined, show_on_website: show },
        }),
      onDone,
    );
  }

  return (
    <form onSubmit={save} className="space-y-4 rounded-xl border border-ink-200 bg-ink-50/60 p-4">
      <p className="text-sm text-ink-500">{text.intro}</p>

      <div className="grid gap-4 sm:grid-cols-2">
        {textFields.map((field) => (
          <Field key={field.key} label={field.label} htmlFor={`${id}-${field.key}`} hint={field.hint} error={errors[field.key]}>
            <Input id={`${id}-${field.key}`} value={draft[field.key]} maxLength={field.max} onChange={(event) => set(field.key, event.target.value)} />
          </Field>
        ))}

        <Field label={text.careerStartYear} htmlFor={`${id}-year`} hint={text.careerStartYearHint} error={errors.career_start_year}>
          <Input
            id={`${id}-year`}
            type="number"
            inputMode="numeric"
            min={1950}
            step={1}
            value={draft.career_start_year}
            onChange={(event) => set('career_start_year', event.target.value)}
          />
        </Field>
      </div>

      <ProfilePhotoField
        staffId={member.id}
        saved={current}
        value={draft.photo_path}
        onChange={(photoPath) => set('photo_path', photoPath)}
        error={errors.photo_path}
      />

      <Field label={text.bio} htmlFor={`${id}-bio`} hint={text.bioHint} error={errors.bio}>
        <Textarea id={`${id}-bio`} value={draft.bio} maxLength={600} onChange={(event) => set('bio', event.target.value)} />
      </Field>

      <Checkbox
        label={text.showOnWebsite}
        hint={text.showOnWebsiteHint}
        checked={show}
        onChange={(event) => setShow(event.target.checked)}
      />

      <FormFooter pending={pending} message={message} onCancel={onDone} />
    </form>
  );
}
