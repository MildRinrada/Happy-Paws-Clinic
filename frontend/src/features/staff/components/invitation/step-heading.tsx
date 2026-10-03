/** The small "ขั้นที่ 1 จาก 2" line and the step's title. */
export function StepHeading({ label, title }: { label: string; title: string }) {
  return (
    <div>
      <p className="text-xs font-medium text-brand-700">{label}</p>
      <h2 className="mt-0.5 text-base font-semibold text-ink-900">{title}</h2>
    </div>
  );
}
