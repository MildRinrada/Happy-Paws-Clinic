import { formatTime } from '@/lib/format/datetime';
import { consultationPage } from '../../content/consultation-page';
import type { Vitals } from '../../types/consultation.types';

/** Each set of readings taken during the visit, oldest first. */
export function VitalsList({ vitals }: { vitals: Vitals[] }) {
  const text = consultationPage.vitals;

  if (vitals.length === 0) return <p className="text-sm text-ink-500">{text.none}</p>;

  return (
    <ul className="divide-y divide-ink-100 text-sm">
      {vitals.map((reading) => (
        <li key={reading.id} className="flex flex-wrap gap-x-4 gap-y-1 py-2">
          <span className="w-12 tabular-nums text-ink-500">{formatTime(reading.recorded_at)}</span>
          {reading.weight_kg != null && <span className="font-medium text-ink-900">{text.short.weight(reading.weight_kg)}</span>}
          {reading.temperature_c != null && <span className="text-ink-800">{text.short.temperature(reading.temperature_c)}</span>}
          {reading.heart_rate_bpm != null && <span className="text-ink-800">{text.short.heartRate(reading.heart_rate_bpm)}</span>}
          {reading.respiratory_rate_bpm != null && <span className="text-ink-800">{text.short.respiratoryRate(reading.respiratory_rate_bpm)}</span>}
          {reading.note && <span className="text-ink-600">{reading.note}</span>}
        </li>
      ))}
    </ul>
  );
}
