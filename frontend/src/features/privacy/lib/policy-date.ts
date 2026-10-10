const effectiveDate = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'UTC',
  day: 'numeric',
  month: 'long',
  year: 'numeric',
});

/**
 * A notice's effective date, `2026-11-01` from the API, as the page prints it: "1 พฤศจิกายน 2569".
 * The value is a calendar date with no time, so it is read at UTC midnight and printed in UTC,
 * which no time zone can move to the day before.
 */
export function formatPolicyDate(date: string): string {
  const parsed = new Date(`${date}T00:00:00Z`);
  return Number.isNaN(parsed.getTime()) ? date : effectiveDate.format(parsed);
}
