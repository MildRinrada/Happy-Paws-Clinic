'use client';

import { useContext, useEffect, useRef, useState } from 'react';
import { SaveBarActionsContext } from '../components/save-bar/save-bar-context';
import { sameValue } from '../lib/same-value';

interface UnsavedChangesOptions<T> {
  /** The `id` given to the `<form>`; the bar's save submits that form. Unique on the page. */
  formId: string;
  /** The card's heading, which the bar names while this form has changes. */
  section: string;
  /** What the form would send now. Compared with what was last saved. */
  value: T;
  pending?: boolean;
  /** True while the form shows a refusal from its last save. */
  failed?: boolean;
  /** False keeps the form's own save button: the bar leaves it alone, the leave question does not. */
  inBar?: boolean;
  /** Puts the form back to `saved`, from the bar's cancel. */
  onReset: (saved: T) => void;
}

/**
 * The form's half of the settings save bar: remembers what was last saved, says whether the form
 * differs from it, and tells the bar so. The form calls `markSaved` with what the backend stored.
 *
 * Outside the settings area there is no bar; `inBar` is then false and the form draws its own
 * save button (`SectionFooter` does this).
 */
export function useUnsavedChanges<T>({ formId, section, value, pending = false, failed = false, inBar = true, onReset }: UnsavedChangesOptions<T>) {
  const { available, register, unregister } = useContext(SaveBarActionsContext);
  const [saved, setSaved] = useState(value);
  const dirty = !sameValue(value, saved);
  const reset = useRef(() => {});

  useEffect(() => {
    reset.current = () => onReset(saved);
  });

  useEffect(() => {
    register({ formId, section, dirty, pending, failed, inBar: inBar && available }, () => reset.current());
  }, [register, formId, section, dirty, pending, failed, inBar, available]);

  useEffect(() => () => unregister(formId), [unregister, formId]);

  return { dirty, inBar: inBar && available, markSaved: (stored: T) => setSaved(stored) };
}
