/** One number of the referral results, with its label under it. */
export function ReferralStat({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-xl bg-site-soft p-3">
      <p className="text-xl font-semibold tabular-nums text-ink-900">{value.toLocaleString('th-TH')}</p>
      <p className="text-xs text-site-muted">{label}</p>
    </div>
  );
}
