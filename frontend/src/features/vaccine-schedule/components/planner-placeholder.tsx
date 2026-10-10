import { vaccinePage } from '../content/vaccine-schedule-page';

/** Beside the form on a wide screen, before it is filled in: where the timeline will appear. */
export function PlannerPlaceholder() {
  const text = vaccinePage.placeholder;

  return (
    <div className="hidden min-h-[320px] flex-col justify-center rounded-2xl border-2 border-dashed border-ink-200 bg-site-soft p-8 text-center lg:flex print:hidden">
      <p className="text-xl font-semibold">{text.heading}</p>
      <p className="mx-auto mt-2 max-w-[420px] text-[15px] text-site-muted">{text.body}</p>
    </div>
  );
}
