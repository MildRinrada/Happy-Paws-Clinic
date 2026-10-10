import { vaccinePage } from '../content/vaccine-schedule-page';

/** Print the timeline, or save it as a PDF from the same window. Only the timeline is printed. */
export function PrintScheduleCard() {
  const text = vaccinePage.actions.print;

  return (
    <li className="flex flex-col rounded-2xl border border-site-border bg-white p-5 sm:p-6">
      <h3 className="text-xl font-bold leading-tight">{text.title}</h3>
      <p className="mt-2 flex-1 text-[15px] leading-relaxed text-site-muted">{text.detail}</p>
      <button
        type="button"
        onClick={() => window.print()}
        className="mt-4 inline-flex h-[52px] w-full items-center justify-center rounded-[10px] bg-black px-5 text-base font-medium text-white transition hover:bg-ink-800 sm:w-auto sm:self-start"
      >
        {text.button}
      </button>
    </li>
  );
}
