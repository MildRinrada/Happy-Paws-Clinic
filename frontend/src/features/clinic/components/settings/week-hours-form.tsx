'use client';

import { useState, type FormEvent } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardHeader } from '@/components/ui/card';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { replaceOpeningHoursAction } from '../../actions/clinic-setup-actions';
import { hoursSettings } from '../../content/hours-settings';
import { dayErrors, weekHoursInput, weekHoursPayload } from '../../lib/week-hours-input';
import type { OpeningHours } from '../../types/clinic.types';
import type { DayHoursInput } from '../../types/settings.types';
import { DayHoursRow } from './day-hours-row';

/** The whole week, saved in one go: the API replaces all seven days together. */
export function WeekHoursForm({ hours }: { hours: OpeningHours }) {
  const text = hoursSettings.week;
  const [days, setDays] = useState(() => weekHoursInput(hours));
  const [version, setVersion] = useState(hours.version);
  const [configured, setConfigured] = useState(hours.configured);
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);
  const perDay = dayErrors(errors);

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
    submit(
      () => replaceOpeningHoursAction({ version, days: weekHoursPayload(days) }),
      (stored) => {
        setVersion(stored.version);
        setConfigured(true);
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
      <form onSubmit={save}>
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
          <div className="flex flex-wrap items-center gap-3">
            <FormStatus message={message ?? errors.days} saved={saved} savedText={formMessages.saved} />
            <Button type="submit" disabled={pending}>
              {pending ? formMessages.saving : formMessages.save}
            </Button>
          </div>
        </div>
      </form>
    </Card>
  );
}
