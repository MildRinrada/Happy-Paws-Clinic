'use client';

import { useState, useTransition } from 'react';
import type { ActionResult } from '@/lib/actions/action-result';
import { fieldErrors } from '@/lib/actions/field-errors';
import { consultationPage } from '../content/consultation-page';

/**
 * The shared half of every add-form on the case: run the action, spread a refusal over the fields
 * it names, keep a general message for the rest, and say when the network itself failed.
 */
export function usePartSubmit() {
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [message, setMessage] = useState<string>();
  const [pending, startTransition] = useTransition();

  function submit<T>(call: () => Promise<ActionResult<T>>, onDone: (data: T) => void, onRefused?: (result: ActionResult<T> & { ok: false }) => boolean) {
    setErrors({});
    setMessage(undefined);

    startTransition(async () => {
      try {
        const result = await call();

        if (result.ok) {
          onDone(result.data);
          return;
        }

        // A form with its own way of showing a refusal (the allergy check) takes it here.
        if (onRefused?.(result)) return;

        const byField = fieldErrors(result.error);
        setErrors(byField);
        if (Object.keys(byField).length === 0) setMessage(result.error.message);
      } catch {
        setMessage(consultationPage.unreachable);
      }
    });
  }

  return { errors, message, pending, submit };
}
