'use client';

import { useState, useTransition } from 'react';
import type { ActionResult } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';

export interface FormSubmitMessages {
  /** Shown on `409 version_conflict`: someone saved this record since the page was loaded. */
  conflict: string;
  /** Shown when the action could not reach the backend at all. */
  unreachable: string;
}

/**
 * The shared half of a settings form: run the action, spread a refusal over the fields it names,
 * keep one general message for the rest, and say plainly when the save went through.
 */
export function useFormSubmit(messages: FormSubmitMessages) {
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [saved, setSaved] = useState(false);
  const [pending, startTransition] = useTransition();

  function submit<T>(call: () => Promise<ActionResult<T>>, onDone?: (data: T) => void) {
    setErrors({});
    setMessage(undefined);
    setSaved(false);

    startTransition(async () => {
      try {
        const result = await call();

        if (result.ok) {
          setSaved(true);
          onDone?.(result.data);
          return;
        }

        if (result.error.code === 'version_conflict') {
          setMessage(messages.conflict);
          return;
        }

        const byField = fieldErrors(result.error);
        setErrors(byField);
        if (Object.keys(byField).length === 0) setMessage(result.error.message);
      } catch {
        setMessage(messages.unreachable);
      }
    });
  }

  /** Clears "saved" once the person starts changing the form again. */
  const touch = () => setSaved(false);

  /** Forgets the last save's outcome, for a form put back to what was last saved. */
  const clear = () => {
    setErrors({});
    setMessage(undefined);
    setSaved(false);
  };

  return { errors, message, saved, pending, submit, touch, clear };
}
