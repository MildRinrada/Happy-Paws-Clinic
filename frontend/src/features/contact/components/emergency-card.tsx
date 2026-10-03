import { contactHref } from '@/features/clinic/lib/contact-links';
import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { contactPage } from '../content/contact-page';

/** The emergency line, first and in red: the one number that should not take a search to find. */
export function EmergencyCard({ contact }: { contact: ClinicContact }) {
  const text = contactPage.emergency;

  return (
    <div className="flex flex-wrap items-center justify-between gap-4 rounded-2xl bg-site-alert px-6 py-5 text-white">
      <div className="min-w-0">
        <p className="text-sm font-semibold uppercase tracking-wide text-white/85">{text.title} / {contact.label}</p>
        <p className="text-3xl font-bold tabular-nums">{contact.value}</p>
        <p className="mt-1 max-w-xl text-sm text-white/90">{text.note}</p>
      </div>
      <a href={contactHref(contact)} className="inline-flex h-12 items-center rounded-xl bg-white px-6 font-bold text-site-alert transition hover:bg-white/90">
        {text.call}
      </a>
    </div>
  );
}
