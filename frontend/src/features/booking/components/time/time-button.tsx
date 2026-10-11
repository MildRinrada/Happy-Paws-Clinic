import { formatTime } from '@/lib/format/datetime';

/** One free time. The one picked is orange; picking does not hold it yet. */
export function TimeButton({ startsAt, selected, disabled, onSelect }: { startsAt: string; selected: boolean; disabled: boolean; onSelect: () => void }) {
  return (
    <button
      type="button"
      onClick={onSelect}
      disabled={disabled}
      aria-pressed={selected}
      className={`h-[45px] w-full rounded-[10px] border text-base transition disabled:cursor-not-allowed disabled:opacity-50 ${
        selected
          ? 'border-site-orange bg-site-orange font-semibold text-ink-900'
          : 'border-ink-200 bg-white font-medium text-ink-900 hover:border-site-forest'
      }`}
    >
      {formatTime(startsAt)}
    </button>
  );
}
