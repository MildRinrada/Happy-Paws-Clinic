'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardHeader } from '@/components/ui/card';
import { SectionFooter } from '@/features/settings/components/section-footer';
import { formMessages } from '@/features/settings/content/form-messages';
import { useUnsavedChanges } from '@/features/settings/hooks/use-unsaved-changes';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceOpeningHoursAction } from '../../actions/clinic-setup-actions';
import { hoursSettings } from '../../content/hours-settings';
import { dayErrors, weekHoursInput, weekHoursPayload } from '../../lib/week-hours-input';
import type { OpeningHours } from '../../types/clinic.types';
import type { DayHoursInput } from '../../types/settings.types';
import { DayHoursRow } from './day-hours-row';

const FORM_ID = 'week-hours-form';

/** The whole week, saved in one go by the settings save bar: the API replaces all seven days together. */
export function WeekHoursForm({ hours }: { hours: OpeningHours }) {
  const text = hoursSettings.week;
  const [days, setDays] = useState(() => weekHoursInput(hours));
  const [version, setVersion] = useState(hours.version);
  const [configured, setConfigured] = useState(hours.configured);
  const { errors, message, saved, pending, submit, touch, clear } = useFormSubmit(formMessages);
  const perDay = dayErrors(errors);
  const changes = useUnsavedChanges({
    formId: FORM_ID,
    section: text.heading,
    value: days,
    pending,
    failed: Boolean(message) || Object.keys(errors).length > 0,
    onReset: (stored) => {
      clear();
      setDays(stored);
    },
  });

  function change(index: number, day: DayHoursInput) {
    touch();
    setDays((current) => current.map((entry, at) => (at === index ? day : entry)));
  }

  function copyMonday() {
    const [monday] = days;
    if (!monday || monday.closed) return;

    touch();
    setDays((current) => current.map((day) => (day.closed ? day : { ...day, opens: monday.opens, closes: monday.closes })));
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const sent = days;

    submit(
      () => replaceOpeningHoursAction({ version, days: weekHoursPayload(sent) }),
      (stored) => {
        setVersion(stored.version);
        setConfigured(true);
        changes.markSaved(sent);
      },
    );
  }

  return (
    <Card>
      <CardHeader
        title={text.heading}
        aside={
          <Button variant="ghost" size="sm" onClick={copyMonday}>
            {text.copyDown}
          </Button>
        }
      />
      <form id={FORM_ID} onSubmit={save}>
        {!configured && (
          <div className="px-5 pt-4">
            <Alert tone="info">{text.notConfigured}</Alert>
          </div>
        )}

        <ul className="divide-y divide-ink-100">
          {days.map((day, index) => (
            <DayHoursRow key={day.day} day={day} error={perDay[index]} onChange={(next) => change(index, next)} />
          ))}
        </ul>

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-ink-100 px-5 py-3">
          <p className="text-xs text-ink-500">{text.note}</p>
          <SectionFooter pending={pending} message={message ?? errors.days} saved={saved} inBar={changes.inBar} />
        </div>
      </form>
    </Card>
  );
}
