interface VisitProgressProps {
  /** How full the bar is, 0 to 1. */
  progress: number;
  steps: string[];
}

/** The bar from check-in to payment, filled to where the visit has got to, with the steps under it. */
export function VisitProgress({ progress, steps }: VisitProgressProps) {
  const percent = Math.round(Math.min(1, Math.max(0, progress)) * 100);

  return (
    <div>
      <div
        role="progressbar"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={percent}
        className="h-3.5 overflow-hidden rounded-full bg-white/25"
      >
        <div
          className="h-full rounded-full bg-gradient-to-r from-green-500 to-site-orange transition-[width] duration-700"
          style={{ width: `${percent}%` }}
        />
      </div>

      <ol className="mt-2 flex justify-between text-xs font-semibold text-white">
        {steps.map((step) => (
          <li key={step}>{step}</li>
        ))}
      </ol>
    </div>
  );
}
