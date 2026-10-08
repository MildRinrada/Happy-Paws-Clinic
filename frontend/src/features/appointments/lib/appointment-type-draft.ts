import type { AppointmentTypeInput, QueuePriority } from '../types/appointment-settings.types';

/** The backend's limits (`AppointmentTypeCatalog`): up to 20 types, `regular` always in the set. */
export const MAX_TYPES = 20;
export const REGULAR = 'regular';
const CODE_PATTERN = /^[a-z][a-z0-9_]{1,29}$/;

/** One type as the form edits it. `key` keeps React's rows steady while rows come and go. */
export interface TypeDraft {
  key: number;
  code: string;
  name: string;
  duration: string;
  onlineBooking: boolean;
  leadTime: string;
  priority: QueuePriority;
  /** The `regular` row: its code stays and it cannot be removed. */
  locked: boolean;
}

export type TypeProblem = 'badCode' | 'duplicateCode' | 'needName' | 'badNumber';

let nextKey = 0;

function draft(code: string, name: string, onlineBooking = true, priority: QueuePriority = 'normal'): TypeDraft {
  return { key: nextKey++, code, name, duration: '', onlineBooking, leadTime: '', priority, locked: code === REGULAR };
}

export function emptyType(): TypeDraft {
  return draft('', '');
}

/**
 * Where the form starts, since the stored set cannot be read: the four the contract names, with the
 * rules the backend gives them by default.
 */
export function defaultTypes(): TypeDraft[] {
  return [
    draft(REGULAR, 'นัดปกติ'),
    draft('emergency', 'ฉุกเฉิน', false, 'emergency'),
    draft('follow_up', 'ติดตามผล'),
    draft('surgery', 'ผ่าตัด', false),
  ];
}

/** The set the backend answered with, back into the form. */
export function draftsFrom(types: AppointmentTypeInput[]): TypeDraft[] {
  return types.map((type) => ({
    key: nextKey++,
    code: type.code,
    name: type.name,
    duration: type.duration_minutes == null ? '' : String(type.duration_minutes),
    onlineBooking: type.online_booking,
    leadTime: type.lead_time_minutes == null ? '' : String(type.lead_time_minutes),
    priority: type.queue_priority,
    locked: type.code === REGULAR,
  }));
}

function optionalWhole(text: string): number | null | undefined {
  const trimmed = text.trim();
  if (!trimmed) return null;
  return /^\d{1,6}$/.test(trimmed) ? Number(trimmed) : undefined;
}

/** What is wrong with each row before it is sent. Ranges are left to the backend. */
export function typeProblems(types: TypeDraft[]): (TypeProblem | undefined)[] {
  const codes = types.map((type) => type.code.trim());

  return types.map((type, at) => {
    const code = codes[at]!;
    if (!CODE_PATTERN.test(code)) return 'badCode';
    if (codes.indexOf(code) !== at) return 'duplicateCode';
    if (!type.name.trim()) return 'needName';
    if (optionalWhole(type.duration) === undefined || optionalWhole(type.leadTime) === undefined) return 'badNumber';
    return undefined;
  });
}

export function toTypeInput(type: TypeDraft): AppointmentTypeInput {
  return {
    code: type.code.trim(),
    name: type.name.trim(),
    duration_minutes: optionalWhole(type.duration) ?? null,
    online_booking: type.onlineBooking,
    lead_time_minutes: optionalWhole(type.leadTime) ?? null,
    queue_priority: type.priority,
  };
}
