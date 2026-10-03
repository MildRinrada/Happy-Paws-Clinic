import Link from 'next/link';
import { footerContact, type FooterDetail, type SocialLink } from '@/config/site-footer';
import { contactHref } from '@/features/clinic/lib/contact-links';
import type { SiteInfo } from '@/features/clinic/lib/load-site-info';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { FooterColumn, FooterColumnTitle } from './footer-column';
import { FooterDetailList } from './footer-detail';
import { FooterSocialLinks } from './footer-social-links';

const asDetail = (contact: ClinicContact): FooterDetail => ({
  label: contact.label,
  lines: [contact.value],
  href: contactHref(contact),
});

/** The second column: the clinic's phones, emergency line, email and chat channels. */
export function FooterContactColumn({ info }: { info: SiteInfo }) {
  const details = [...info.phones, info.emergency, info.email].filter((contact): contact is ClinicContact => Boolean(contact));
  const socials: SocialLink[] = info.socials
    .filter((contact) => contactHref(contact))
    .map((contact) => ({ name: contact.label, icon: contact.kind as SocialLink['icon'], href: contactHref(contact)! }));

  return (
    <FooterColumn heading={<FooterColumnTitle>{footerContact.title}</FooterColumnTitle>}>
      <FooterDetailList details={details.map(asDetail)} />

      {socials.length > 0 && (
        <div className="mt-3.5">
          <FooterSocialLinks label={footerContact.chatLabel} links={socials} />
        </div>
      )}

      <Link href={footerContact.more.href} className="mt-3.5 inline-block text-[13px] underline-offset-2 hover:underline">
        {footerContact.more.label}
      </Link>
    </FooterColumn>
  );
}
