const CLINIC_OFFSET_MS = 7 * 60 * 60 * 1000;

/**
 * "09:00" for an instant, read on the clinic's wall clock. Bangkok has no daylight saving, so a
 * fixed offset is exact, and the same string comes out on the server and in any browser: no
 * `Intl` data is involved, which keeps the roster's client parts free of hydration mismatches.
 */
export function clinicClock(iso: string): string {
  return new Date(Date.parse(iso) + CLINIC_OFFSET_MS).toISOString().slice(11, 16);
}

/** The clinic day an instant falls on, `YYYY-MM-DD`, by the same fixed offset. */
export function clinicDate(iso: string): string {
  return new Date(Date.parse(iso) + CLINIC_OFFSET_MS).toISOString().slice(0, 10);
}
