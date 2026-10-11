import { PageBanner } from '@/components/site/page-banner';
import { contactPage } from '../content/contact-page';

/** The top of the page: what it is for, in one heading and one paragraph. */
export function ContactHero() {
  return <PageBanner eyebrow={contactPage.eyebrow} heading={contactPage.heading} lead={contactPage.lead} />;
}
