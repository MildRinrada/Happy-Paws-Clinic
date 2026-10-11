import { SiteCard } from '@/components/site/site-card';
import { ctaLinkLook } from '@/components/ui/cta-link';
import { vaccinePage } from '../content/vaccine-schedule-page';

/** Print the timeline, or save it as a PDF from the same window. Only the timeline is printed. */
export function PrintScheduleCard() {
  const text = vaccinePage.actions.print;

  return (
    <SiteCard as="li" padding="lg" className="flex flex-col">
      <h3 className="text-xl font-bold leading-tight">{text.title}</h3>
      <p className="mt-2 flex-1 text-[15px] leading-relaxed text-site-muted">{text.detail}</p>
      <button
        type="button"
        onClick={() => window.print()}
        className={`mt-4 w-full sm:w-auto sm:self-start ${ctaLinkLook('dark', 'lg')}`}
      >
        {text.button}
      </button>
    </SiteCard>
  );
}
