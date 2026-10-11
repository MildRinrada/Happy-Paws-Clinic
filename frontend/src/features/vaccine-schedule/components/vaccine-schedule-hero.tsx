import { PageBanner } from '@/components/site/page-banner';
import { vaccinePage } from '../content/vaccine-schedule-page';

/** The top of the page: what the tool does, in one heading and one paragraph. Not printed. */
export function VaccineScheduleHero() {
  const text = vaccinePage.hero;

  return (
    <PageBanner eyebrow={text.eyebrow} heading={text.heading} lead={text.lead} leadWidth="roomy" className="print:hidden" />
  );
}
