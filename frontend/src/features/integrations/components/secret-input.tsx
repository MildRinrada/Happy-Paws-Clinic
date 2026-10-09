'use client';

import { useId } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Field, Input } from '@/components/ui/field';
import { providerShared } from '../content/provider-settings';
import type { SecretDraft } from '../types/integration.types';

interface SecretInputProps {
  label: string;
  stored: boolean;
  draft: SecretDraft;
  error?: string;
  onChange: (draft: SecretDraft) => void;
}

/**
 * A write-only credential. What is stored is never shown: an empty field keeps it, something typed
 * replaces it, and the box under it clears it.
 */
export function SecretInput({ label, stored, draft, error, onChange }: SecretInputProps) {
  const id = useId();

  return (
    <div className="space-y-2">
      <Field label={label} htmlFor={id} hint={providerShared.secretHint} error={error}>
        <Input
          id={id}
          type="password"
          autoComplete="off"
          maxLength={500}
          placeholder={stored ? providerShared.storedPlaceholder : providerShared.emptyPlaceholder}
          value={draft.value}
          disabled={draft.clear}
          onChange={(event) => onChange({ ...draft, value: event.target.value })}
        />
      </Field>
      {stored && (
        <Checkbox
          label={providerShared.clear}
          checked={draft.clear}
          onChange={(event) => onChange({ value: '', clear: event.target.checked })}
        />
      )}
    </div>
  );
}
