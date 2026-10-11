import { SiteContainer } from '@/components/layout/site-container';
import { SectionHeading } from '@/components/site/section-heading';
import { SiteCard } from '@/components/site/site-card';
import { footerLegal } from '@/config/site-footer';
import { aboutLicence } from '../content/about-page';

/**
 * The licences a veterinary clinic must show, in full. The same entries as the footer, read from
 * the footer's config, so they are kept up to date in one place.
 */
export function LicenceSection() {
  return (
    <section aria-labelledby="licence-heading" className="py-12">
      <SiteContainer>
        <SectionHeading id="licence-heading">{aboutLicence.heading}</SectionHeading>
        <p className="mt-2 text-[15px] text-site-muted">{aboutLicence.lead}</p>

        <dl className="mt-6 grid gap-4 sm:grid-cols-2">
          {footerLegal.details.map((detail) => (
            <SiteCard key={detail.label} tone="whiteSoft">
              <dt className="text-sm text-site-muted">{detail.label}</dt>
              <dd className="mt-1 font-semibold text-ink-900">
                {detail.lines.map((line) => (
                  <span key={line} className="block">
                    {line}
                  </span>
                ))}
              </dd>
            </SiteCard>
          ))}
        </dl>
      </SiteContainer>
    </section>
  );
}
