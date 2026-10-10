import { SiteContainer } from '@/components/layout/site-container';
import { emergencyPage } from '../content/emergency-page';
import type { CallNumber } from '../types/emergency.types';
import { CallList } from './call-list';

/** The top of the page, in red: what to do first, and the numbers to ring. */
export function EmergencyHero({ numbers }: { numbers: CallNumber[] }) {
  const text = emergencyPage.hero;

  return (
    <section aria-labelledby="emergency-heading" className="bg-site-alert text-white">
      <SiteContainer className="py-9 lg:py-12">
        <p className="text-sm font-semibold uppercase tracking-wide text-white/85">{text.eyebrow}</p>
        <h1 id="emergency-heading" className="mt-2 max-w-[720px] text-3xl font-semibold leading-tight sm:text-[38px]">
          {text.heading}
        </h1>
        <p className="mt-4 max-w-[600px] text-base leading-relaxed text-white/95">{text.lead}</p>

        <div className="mt-7">
          <CallList numbers={numbers} />
        </div>
      </SiteContainer>
    </section>
  );
}
