'use client';

import { useEffect, useRef, useState } from 'react';
import { clinicDay } from '@/lib/format/datetime';
import { vaccinePage } from '../content/vaccine-schedule-page';
import { scheduleRules, vaccinePlans } from '../content/vaccine-plans';
import { buildSchedule } from '../lib/build-schedule';
import { validateInput, type InputErrors } from '../lib/validate-input';
import type { PetSchedule, ScheduleActions, ScheduleInput, Species } from '../types/vaccine-schedule.types';
import { PlannerForm } from './planner-form';
import { PlannerPlaceholder } from './planner-placeholder';
import { ScheduleResult } from './schedule-result';

interface Result {
  input: ScheduleInput;
  today: string;
  schedule: PetSchedule;
}

/**
 * The helper itself: the form, and the timeline once it is filled in.
 *
 * Everything happens in the browser. The dates are never sent, stored or put in the address, and
 * "today" is read only when the visitor asks for a timeline, so the server's render and the
 * browser's first render are the same empty form.
 */
export function SchedulePlanner({ actions }: { actions: ScheduleActions }) {
  const [species, setSpecies] = useState<Species>('dog');
  const [birthDate, setBirthDate] = useState('');
  const [lastVaccineDate, setLastVaccineDate] = useState('');
  const [errors, setErrors] = useState<InputErrors>({});
  const [result, setResult] = useState<Result | null>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);

  useEffect(() => {
    if (result) {
      headingRef.current?.focus();
    }
  }, [result]);

  const submit = () => {
    const today = clinicDay(new Date());
    const found = validateInput(birthDate, lastVaccineDate, today);
    setErrors(found);

    if (found.birthDate || found.lastVaccineDate) {
      return;
    }

    const input: ScheduleInput = { species, birthDate, lastVaccineDate: lastVaccineDate || undefined };
    const schedule = buildSchedule(vaccinePlans[species], { ...input, today }, scheduleRules, {
      title: vaccinePage.result.catchUpTitle,
      note: vaccinePage.result.catchUpNote,
    });

    setResult({ input, today, schedule });
  };

  return (
    <div className="grid gap-8 lg:grid-cols-[380px_1fr] lg:items-start print:block">
      <div className="lg:sticky lg:top-6 print:hidden">
        <PlannerForm
          species={species}
          birthDate={birthDate}
          lastVaccineDate={lastVaccineDate}
          errors={errors}
          onSpecies={setSpecies}
          onBirthDate={setBirthDate}
          onLastVaccineDate={setLastVaccineDate}
          onSubmit={submit}
        />
      </div>

      <div className="min-w-0">
        {result ? (
          <ScheduleResult
            input={result.input}
            today={result.today}
            schedule={result.schedule}
            actions={actions}
            headingRef={headingRef}
          />
        ) : (
          <PlannerPlaceholder />
        )}
      </div>
    </div>
  );
}
