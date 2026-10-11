'use client';

import { useState, useTransition } from 'react';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { copyWeekAction } from '../../actions/schedule-actions';
import { copyWeekText } from '../../content/copy-week';
import { schedulePage } from '../../content/schedule-page';
import type { CopyWeekPlan, CopyWeekResult, ShiftClash } from '../../types/schedule.types';
import { CopyWeekClashes } from './copy-week-clashes';

interface CopyWeekFormProps {
  branchId: string;
  plan: CopyWeekPlan;
  names: Record<string, string>;
  onDone: () => void;
}

/**
 * "คัดลอกเวรสัปดาห์ก่อน" (docs/contract-extensions.md § 49): says what will be copied and the
 * rules, then copies all or nothing. A refusal lists each clash with whose it is.
 */
export function CopyWeekForm({ branchId, plan, names, onDone }: CopyWeekFormProps) {
  const text = copyWeekText;
  const [copied, setCopied] = useState<CopyWeekResult>();
  const [clashes, setClashes] = useState<ShiftClash[]>();
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();

  function copy() {
    setMessage(undefined);
    setClashes(undefined);
    startTransition(async () => {
      try {
        const result = await copyWeekAction({ branch_id: branchId, from_week: plan.fromWeek, to_week: plan.toWeek });

        if (result.ok) {
          setCopied(result.data);
          return;
        }

        const listed = result.error.details?.conflicts ?? result.error.details?.time_off;
        if (Array.isArray(listed)) setClashes(listed as ShiftClash[]);
        else setMessage(result.error.message);
      } catch {
        setMessage(formMessages.unreachable);
      }
    });
  }

  if (copied) {
    return (
      <div className="space-y-3">
        <Alert tone="success">
          {text.done(copied.data.length)}
          {copied.skipped_started > 0 && <span className="block">{text.skipped(copied.skipped_started)}</span>}
        </Alert>
        <div className="flex justify-end">
          <Button variant="secondary" onClick={onDone}>
            {schedulePage.dialogs.close}
          </Button>
        </div>
      </div>
    );
  }

  if (plan.shifts === 0) {
    return <p className="text-sm text-ink-600">{text.nothing}</p>;
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-ink-800">{text.summary(plan.shifts, plan.people, plan.fromLabel, plan.toLabel)}</p>
      <ul className="list-disc space-y-1 pl-5 text-sm text-ink-600">
        {text.rules.map((rule) => (
          <li key={rule}>{rule}</li>
        ))}
      </ul>
      {clashes && <CopyWeekClashes clashes={clashes} names={names} />}
      <div className="flex flex-wrap items-center justify-end gap-3">
        <FormStatus message={message} savedText="" />
        <Button variant="ghost" disabled={pending} onClick={onDone}>
          {text.cancel}
        </Button>
        <Button disabled={pending} onClick={copy}>
          {pending ? text.submitting : text.submit}
        </Button>
      </div>
    </div>
  );
}
