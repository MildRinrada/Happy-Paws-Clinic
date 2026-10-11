import { SectionHeading } from '@/components/site/section-heading';
import { warningSigns } from '../content/emergency-care';
import { emergencyPage } from '../content/emergency-page';
import { WarningSignItem } from './warning-sign-item';

/** The signs that mean "go now", as plain text in the page so they read without JavaScript. */
export function WarningSignsSection() {
  const text = emergencyPage.signs;

  return (
    <section aria-labelledby="warning-signs-heading" className="space-y-4">
      <div>
        <SectionHeading id="warning-signs-heading">{text.heading}</SectionHeading>
        <p className="mt-2 text-[15px] text-site-muted">{text.lead}</p>
      </div>

      <ul className="grid gap-3 sm:grid-cols-2">
        {warningSigns.map((sign) => (
          <WarningSignItem key={sign.id} sign={sign} />
        ))}
      </ul>
    </section>
  );
}
