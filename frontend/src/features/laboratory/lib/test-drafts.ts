import type { LabTest } from '../types/lab.types';

/** One row of the order form, as typed. */
export interface TestDraft {
  key: string;
  name: string;
  code: string;
}

export const emptyTest = (): TestDraft => ({ key: crypto.randomUUID(), name: '', code: '' });

/** The tests to send: rows with a name, trimmed. A code with no name is dropped with its row. */
export function testsFrom(drafts: TestDraft[]): LabTest[] {
  return drafts
    .filter((draft) => draft.name.trim())
    .map((draft) => ({ name: draft.name.trim(), code: draft.code.trim() || undefined }));
}
