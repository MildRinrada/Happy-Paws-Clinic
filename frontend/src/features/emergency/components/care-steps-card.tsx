import { useId } from 'react';
import type { CareStep } from '../types/emergency.types';

interface CareStepsCardProps {
  heading: string;
  steps: CareStep[];
  /** `do` draws a tick on green, `dont` a cross on red. */
  tone: 'do' | 'dont';
}

const marks = {
  do: { path: 'M9 16.2 4.8 12l-1.4 1.4L9 19 21 7l-1.4-1.4L9 16.2Z', className: 'bg-stat-green/10 text-stat-green' },
  dont: {
    path: 'M19 6.4 17.6 5 12 10.6 6.4 5 5 6.4 10.6 12 5 17.6 6.4 19 12 13.4 17.6 19 19 17.6 13.4 12 19 6.4Z',
    className: 'bg-site-alert/10 text-site-alert',
  },
};

/** One list of steps under its heading: what to do on the way, or what not to do. */
export function CareStepsCard({ heading, steps, tone }: CareStepsCardProps) {
  const headingId = useId();
  const mark = marks[tone];

  return (
    <section aria-labelledby={headingId} className="rounded-2xl border border-site-border/60 bg-white p-5 sm:p-6">
      <h3 id={headingId} className="text-lg font-bold text-ink-900">
        {heading}
      </h3>

      <ul className="mt-4 space-y-4">
        {steps.map((step) => (
          <li key={step.id} className="flex gap-3">
            <span aria-hidden="true" className={`mt-0.5 inline-flex size-7 shrink-0 items-center justify-center rounded-full ${mark.className}`}>
              <svg viewBox="0 0 24 24" className="size-4" fill="currentColor">
                <path d={mark.path} />
              </svg>
            </span>
            <div className="min-w-0">
              <p className="font-semibold text-ink-900">{step.title}</p>
              <p className="mt-0.5 text-[15px] leading-relaxed text-ink-700">{step.detail}</p>
            </div>
          </li>
        ))}
      </ul>
    </section>
  );
}
