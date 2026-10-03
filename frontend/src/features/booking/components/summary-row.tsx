/** One line of the confirmation summary. Left out when there is nothing to say. */
export function SummaryRow({ label, value }: { label: string; value?: string }) {
  if (!value) return null;

  return (
    <div className="flex justify-between gap-4 py-2">
      <dt className="text-sm text-site-muted">{label}</dt>
      <dd className="text-right text-sm font-medium text-ink-900">{value}</dd>
    </div>
  );
}
