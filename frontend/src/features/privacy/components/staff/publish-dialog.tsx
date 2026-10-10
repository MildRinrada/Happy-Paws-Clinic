'use client';

import { useRouter } from 'next/navigation';
import { useId, useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { clinicDay } from '@/lib/format/datetime';
import { publishPolicyAction } from '../../actions/manage-privacy-policy-actions';
import { publishText } from '../../content/staff-privacy-policy';
import type { StaffPolicyVersion } from '../../types/staff-privacy-policy.types';

interface PublishDialogProps {
  draft: StaffPolicyVersion;
  /** True while the editor holds changes not saved yet: what would be published is not what is on screen. */
  unsaved: boolean;
}

/**
 * Publishing, for the owner (`settings:danger`) only. Opens in place rather than in a pop-up, and
 * asks for the label, the effective date, the lawyer's review and an explicit "I understand this
 * cannot be undone" before the button works.
 */
export function PublishDialog({ draft, unsaved }: PublishDialogProps) {
  const text = publishText;
  const id = useId();
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [label, setLabel] = useState('');
  const [effectiveDate, setEffectiveDate] = useState(() => clinicDay(new Date()));
  const [legalReviewDone, setLegalReviewDone] = useState(false);
  const [confirmed, setConfirmed] = useState(false);
  const { errors, message, saved, pending, submit } = useFormSubmit(formMessages);
  const field = (name: string) => `${id}-${name}`;

  function publish(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!confirmed || unsaved) return;

    submit(
      () =>
        publishPolicyAction({
          id: draft.id,
          version: draft.version,
          label: label.trim(),
          effective_date: effectiveDate,
          legal_review_done: legalReviewDone,
        }),
      () => router.refresh(),
    );
  }

  return (
    <Card>
      <CardHeader title={text.heading} />
      <CardBody className="space-y-4">
        <p id={field('intro')} className="text-sm leading-relaxed text-ink-700">
          {text.intro}
        </p>

        {!open ? (
          <Button onClick={() => setOpen(true)}>{text.open}</Button>
        ) : (
          <form onSubmit={publish} noValidate className="space-y-4" aria-describedby={field('intro')}>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label={text.label} htmlFor={field('label')} hint={text.labelHint} error={errors.label}>
                <Input
                  id={field('label')}
                  value={label}
                  maxLength={32}
                  spellCheck={false}
                  autoCapitalize="none"
                  className="font-mono"
                  aria-invalid={Boolean(errors.label)}
                  aria-describedby={errors.label ? `${field('label')}-error` : undefined}
                  onChange={(event) => setLabel(event.target.value)}
                />
              </Field>
              <Field label={text.effectiveDate} htmlFor={field('date')} hint={text.effectiveHint} error={errors.effective_date}>
                <Input
                  id={field('date')}
                  type="date"
                  value={effectiveDate}
                  min={clinicDay(new Date())}
                  aria-invalid={Boolean(errors.effective_date)}
                  aria-describedby={errors.effective_date ? `${field('date')}-error` : undefined}
                  onChange={(event) => setEffectiveDate(event.target.value)}
                />
              </Field>
            </div>

            <Checkbox
              label={text.legalReview}
              hint={text.legalReviewHint}
              checked={legalReviewDone}
              onChange={(event) => setLegalReviewDone(event.target.checked)}
            />
            <Checkbox label={text.confirm} checked={confirmed} onChange={(event) => setConfirmed(event.target.checked)} />

            {unsaved && (
              <p role="alert" className="text-sm text-red-700">
                {text.unsaved}
              </p>
            )}

            <div className="flex flex-wrap items-center justify-end gap-3 border-t border-ink-100 pt-4">
              <FormStatus message={message} saved={saved} savedText={text.done} />
              <Button variant="ghost" disabled={pending} onClick={() => setOpen(false)}>
                {text.cancel}
              </Button>
              <Button type="submit" disabled={pending || !confirmed || unsaved || label.trim().length === 0}>
                {pending ? text.publishing : text.submit}
              </Button>
            </div>
          </form>
        )}
      </CardBody>
    </Card>
  );
}
