import { emergency } from '@/config/site-footer';
import { contactHref } from '@/features/clinic/lib/contact-links';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { SiteContainer } from '../site-container';

/** The red bar above the footer: the number to ring when it cannot wait for an appointment. */
export function EmergencyBar({ contact }: { contact: ClinicContact }) {
  return (
    <aside aria-label={emergency.label} className="bg-site-alert py-4 text-white lg:py-0">
      <SiteContainer className="flex flex-wrap items-center gap-x-5 gap-y-2 lg:min-h-16">
        <p className="flex h-[26px] items-center whitespace-nowrap rounded-md bg-[#fde9e4] px-3.5 text-[13px] text-[#e60000]">
          {emergency.label}
        </p>

        <a href={contactHref(contact)} className="text-2xl font-bold hover:underline">
          {contact.value}
        </a>

        <p className="w-full text-[13px] lg:ml-auto lg:w-auto lg:text-right">{emergency.note}</p>
      </SiteContainer>
    </aside>
  );
}
