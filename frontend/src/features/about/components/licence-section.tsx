import { SiteContainer } from '@/components/layout/site-container';
import { footerLegal } from '@/config/site-footer';
import { SectionHeading } from '@/features/home/components/shared/section-heading';
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
            <div key={detail.label} className="rounded-2xl border border-site-border/60 p-5">
              <dt className="text-sm text-site-muted">{detail.label}</dt>
              <dd className="mt-1 font-semibold text-ink-900">
                {detail.lines.map((line) => (
                  <span key={line} className="block">
                    {line}
                  </span>
                ))}
              </dd>
            </div>
          ))}
        </dl>
      </SiteContainer>
    </section>
  );
}
