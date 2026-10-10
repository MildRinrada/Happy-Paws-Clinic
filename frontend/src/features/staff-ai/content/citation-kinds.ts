import type { CitationKind } from '../types/staff-ai.types';

/** What each citation chip says before its date. */
export const citationKinds: Record<CitationKind, string> = {
  allergy: 'แพ้ยา',
  visit: 'ตรวจ',
  medicine: 'ยา',
  weight: 'น้ำหนัก',
  lab: 'แล็บ',
  vaccination: 'วัคซีน',
};
