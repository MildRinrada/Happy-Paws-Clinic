import { formatDateTime } from '@/lib/format/datetime';
import { petProfile } from '../../content/pet-profile';
import type { PetTimelineEntry } from '../../types/pet.types';

const kindTone: Record<string, string> = {
  visit: 'bg-brand-600',
  medicine: 'bg-violet-500',
  vaccination: 'bg-emerald-500',
  lab: 'bg-sky-500',
};

/** One event in the patient's life at the clinic, on the line drawn by the list. */
export function TimelineEntry({ entry }: { entry: PetTimelineEntry }) {
  const kinds = petProfile.timeline.kinds;

  return (
    <li className="relative pl-6">
      <span aria-hidden="true" className={`absolute left-0 top-1.5 size-3 rounded-full ring-4 ring-white ${kindTone[entry.kind] ?? 'bg-ink-400'}`} />
      <p className="text-xs text-ink-500">
        <span className="font-semibold text-ink-700">{kinds[entry.kind] ?? entry.kind}</span> / {formatDateTime(entry.occurred_at)}
      </p>
      <p className="text-sm text-ink-900">{entry.summary}</p>
    </li>
  );
}
