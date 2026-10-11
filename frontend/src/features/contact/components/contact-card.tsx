import { SiteCard } from '@/components/site/site-card';
import { contactHref } from '@/features/clinic/lib/contact-links';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { contactPage } from '../content/contact-page';
import { ContactKindIcon } from './contact-kind-icon';

/** One way to reach the clinic: what it is, the number or name, and the button that uses it. */
export function ContactCard({ contact }: { contact: ClinicContact }) {
  const href = contactHref(contact);
  const external = href?.startsWith('http');

  return (
    <SiteCard as="li" tone="whiteSoft" className="flex items-start gap-4">
      <ContactKindIcon kind={contact.kind} className="size-10 shrink-0" />
      <div className="min-w-0 flex-1">
        <p className="text-sm text-site-muted">{contact.label}</p>
        <p className="text-base font-semibold text-ink-900 [overflow-wrap:anywhere]">{contact.value}</p>
        {href && (
          <a
            href={href}
            {...(external ? { target: '_blank', rel: 'noreferrer' } : {})}
            className="mt-1 inline-block text-sm font-semibold text-site-teal hover:underline"
          >
            {contactPage.actions[contact.kind]}
          </a>
        )}
      </div>
    </SiteCard>
  );
}
