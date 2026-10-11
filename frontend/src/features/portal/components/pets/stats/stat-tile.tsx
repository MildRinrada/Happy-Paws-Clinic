import Link from 'next/link';

/** `alert` is the red tile of an overdue vaccine; `warn` the yellow one of a vaccine due soon. */
export type StatTileTone = 'plain' | 'alert' | 'warn';

const tones: Record<StatTileTone, { box: string; label: string; value: string; link: string }> = {
  plain: { box: 'border-ink-200 bg-white', label: 'text-ink-600', value: 'text-ink-900', link: 'text-brand-600 hover:text-brand-700' },
  alert: { box: 'border-red-200 bg-red-50', label: 'text-red-800', value: 'text-red-900', link: 'text-red-800 hover:text-red-900' },
  warn: { box: 'border-amber-200 bg-amber-50', label: 'text-amber-900', value: 'text-amber-950', link: 'text-amber-900 hover:text-amber-950' },
};

interface StatTileProps {
  label: string;
  value: string;
  tone?: StatTileTone;
  /** The value's own colour, for the weight's dark green. */
  valueClassName?: string;
  /** A quiet line under the value, such as the date it was weighed. */
  note?: string;
  /** A link under the value, such as "จองนัดฉีด". */
  link?: { label: string; href: string };
}

/** One of the three tiles across the top of a pet's page, from 1024px. */
export function StatTile({ label, value, tone = 'plain', valueClassName, note, link }: StatTileProps) {
  const look = tones[tone];

  return (
    <div className={`min-w-0 rounded-2xl border px-4 py-3 ${look.box}`}>
      <p className={`truncate text-[13px] leading-5 ${look.label}`}>{label}</p>
      <p className={`mt-0.5 truncate font-display text-[22px] font-semibold leading-7 ${valueClassName ?? look.value}`}>{value}</p>
      {note && <p className="mt-0.5 truncate text-xs leading-5 text-ink-600">{note}</p>}
      {link && (
        <Link
          href={link.href}
          className={`mt-0.5 inline-block font-display text-[13px] font-medium leading-5 underline decoration-1 underline-offset-4 ${look.link}`}
        >
          {link.label}
        </Link>
      )}
    </div>
  );
}
