interface VisitProgressProps {
  steps: string[];
  /** The step the visit is at, from 0. The ones before it are done. */
  current: number;
  label: string;
}

const bar = {
  done: 'bg-brand-300',
  current: 'bg-site-sun',
  next: 'bg-white/25',
};

/** Check-in to payment as four short bars: done in mint, the current one in yellow, the rest dim. */
export function VisitProgress({ steps, current, label }: VisitProgressProps) {
  return (
    <ol aria-label={label} className="grid grid-cols-4 gap-1.5">
      {steps.map((step, index) => {
        const state = index < current ? 'done' : index === current ? 'current' : 'next';

        return (
          <li key={step} aria-current={state === 'current' ? 'step' : undefined}>
            <span aria-hidden="true" className={`block h-1.5 rounded-full ${bar[state]}`} />
            <span
              className={`mt-1.5 block truncate text-xs leading-5 ${
                state === 'current' ? 'font-semibold text-white' : 'text-brand-100'
              }`}
            >
              {step}
            </span>
          </li>
        );
      })}
    </ol>
  );
}
