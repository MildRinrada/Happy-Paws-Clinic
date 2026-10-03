/** One of the two small boxes: "คิวของคุณ A-14", "เหลืออีก 3 คิว". */
export function QueueStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-[76px] rounded-xl bg-white/15 px-3 py-2 text-center">
      <p className="text-xs font-semibold text-white/90">{label}</p>
      <p className="mt-0.5 whitespace-nowrap font-display text-xl font-bold text-white">{value}</p>
    </div>
  );
}
