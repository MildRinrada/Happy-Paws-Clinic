import { PageBanner } from '@/components/site/page-banner';
import { emergencyPage } from '../content/emergency-page';
import type { CallNumber } from '../types/emergency.types';
import { CallList } from './call-list';

/** The top of the page, in red: what to do first, and the numbers to ring. */
export function EmergencyHero({ numbers }: { numbers: CallNumber[] }) {
  const text = emergencyPage.hero;

  return (
    <PageBanner size="urgent" tone="alert" eyebrow={text.eyebrow} heading={text.heading} lead={text.lead}>
      <div className="mt-7">
        <CallList numbers={numbers} />
      </div>
    </PageBanner>
  );
}
