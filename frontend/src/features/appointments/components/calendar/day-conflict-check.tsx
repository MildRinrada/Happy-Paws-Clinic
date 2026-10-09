'use client';

import { useState, useTransition } from 'react';
import { Button } from '@/components/ui/button';
import { checkDayConflictsAction } from '../../actions/advanced-booking-actions';
import { dayConflictText } from '../../content/advanced-booking';
import type { ConflictReport } from '../../types/advanced-booking.types';
import { DayConflictList } from './day-conflict-list';

interface DayConflictCheckProps {
  day: string;
  branchId: string;
  /** Appointment id to the line a person recognises it by, from the day already on screen. */
  labels: Record<string, string>;
}

/**
 * "ตรวจนัดชนทั้งวัน": asks once, on a press, about every visit already booked on the day in view.
 * The result belongs to the day it was asked for and is dropped when the day changes.
 */
export function DayConflictCheck({ day, branchId, labels }: DayConflictCheckProps) {
  const text = dayConflictText;
  const [result, setResult] = useState<{ day: string; report: ConflictReport }>();
  const [error, setError] = useState<string>();
  const [checking, startCheck] = useTransition();
  const report = result?.day === day ? result.report : undefined;

  function check() {
    setError(undefined);
    startCheck(async () => {
      try {
        const answer = await checkDayConflictsAction({ date: day, branchId });
        if (answer.ok) setResult({ day, report: answer.data });
        else setError(answer.error.message);
      } catch {
        setError(text.unreachable);
      }
    });
  }

  return (
    <section className="space-y-2 rounded-lg border border-ink-200 bg-ink-50/40 px-3.5 py-3">
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="secondary" size="sm" disabled={checking} onClick={check}>
          {checking ? text.checking : text.check}
        </Button>
        {report?.clear && <p role="status" className="text-sm font-medium text-emerald-700">{text.clear}</p>}
        {report && !report.clear && <p role="status" className="text-sm font-medium text-amber-800">{text.found(report.conflicts.length)}</p>}
        {!report && <p className="text-xs text-ink-500">{text.hint}</p>}
      </div>
      {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
      {report && !report.clear && (
        <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2.5">
          <DayConflictList conflicts={report.conflicts} labels={labels} />
        </div>
      )}
      {report && <p className="text-xs text-ink-500">{text.reportOnly}</p>}
    </section>
  );
}
