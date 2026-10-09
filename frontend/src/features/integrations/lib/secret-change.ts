import type { SecretDraft } from '../types/integration.types';

/**
 * What a write-only field sends: nothing when left empty (keep what is stored), an empty string
 * when cleared, the typed value otherwise.
 */
export function secretChange(draft: SecretDraft): string | undefined {
  if (draft.clear) {
    return '';
  }

  const typed = draft.value.trim();
  return typed.length > 0 ? typed : undefined;
}

export const emptySecret: SecretDraft = { value: '', clear: false };
