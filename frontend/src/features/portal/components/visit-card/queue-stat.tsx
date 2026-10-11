import type { VisitStat } from '../../lib/visit-stats';

/** One of the two boxes: "คิวของคุณ A12", "เหลืออีก 2 คิว". */
export function QueueStat({ label, value, unit, quiet }: VisitStat) {
  return (
    <div className="min-w-0 rounded-xl bg-white/10 px-3.5 pb-4 pt-3">
      <p className="text-[13px] leading-5 text-brand-100">{label}</p>
      <p className="flex h-12 items-baseline gap-3 font-display font-bold text-white">
        <span className={`whitespace-nowrap leading-[48px] ${quiet ? 'text-2xl' : 'text-[40px]'}`}>{value}</span>
        {unit && <span className="text-lg font-semibold">{unit}</span>}
      </p>
    </div>
  );
}
