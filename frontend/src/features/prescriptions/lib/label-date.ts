const day = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'Asia/Bangkok',
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

/**
 * The label's `dispensed_on` is a clinic calendar day, `YYYY-MM-DD`. Read as midnight UTC it is
 * 07:00 the same day in Bangkok, so the day printed is the day sent.
 */
export function formatLabelDate(clinicDay: string): string {
  const parsed = new Date(`${clinicDay}T00:00:00Z`);

  return Number.isNaN(parsed.getTime()) ? clinicDay : day.format(parsed);
}
