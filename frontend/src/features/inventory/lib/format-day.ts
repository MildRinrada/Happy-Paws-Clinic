const day = new Intl.DateTimeFormat('th-TH', {
  timeZone: 'Asia/Bangkok',
  day: 'numeric',
  month: 'short',
  year: '2-digit',
});

/**
 * An expiry or delivery date is a clinic calendar day, `YYYY-MM-DD`. Read as midnight UTC it is
 * 07:00 the same day in Bangkok, so the day shown is the day sent.
 */
export function formatDay(clinicDay: string | undefined): string {
  if (!clinicDay) {
    return '-';
  }

  const parsed = new Date(`${clinicDay}T00:00:00Z`);

  return Number.isNaN(parsed.getTime()) ? clinicDay : day.format(parsed);
}
