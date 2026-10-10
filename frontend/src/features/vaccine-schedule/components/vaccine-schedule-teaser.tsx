import { CtaLink } from '@/components/ui/cta-link';
import { vaccinePage } from '../content/vaccine-schedule-page';

/** The card on the home page that sends owners to the vaccine schedule helper. */
export function VaccineScheduleTeaser() {
  const text = vaccinePage.teaser;

  return (
    <aside
      aria-label={text.heading}
      className="mt-5 flex flex-col gap-4 rounded-[14px] border border-site-teal/40 bg-brand-50 px-5 py-5 sm:flex-row sm:items-center sm:justify-between xl:mt-7"
    >
      <div>
        <p className="text-lg font-bold leading-tight">{text.heading}</p>
        <p className="mt-1 text-[15px] text-site-muted">{text.lead}</p>
      </div>
      <CtaLink href={text.action.href} size="lg" className="shrink-0">
        {text.action.label}
      </CtaLink>
    </aside>
  );
}
