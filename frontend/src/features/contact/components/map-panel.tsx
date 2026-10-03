import type { ClinicContact } from '@/features/clinic/types/contact.types';
import { contactPage } from '../content/contact-page';

/** One map entry: the live map when it has an embed, the address, and the link out to Google Maps. */
export function MapPanel({ map, title }: { map: ClinicContact; title?: string }) {
  return (
    <figure className="overflow-hidden rounded-2xl border border-site-border/60 bg-white">
      {map.embed_url && (
        <iframe
          src={map.embed_url}
          title={title ?? map.label}
          loading="lazy"
          referrerPolicy="no-referrer-when-downgrade"
          className="aspect-[16/9] w-full border-0"
        />
      )}
      <figcaption className="space-y-1 p-5">
        <p className="text-sm text-site-muted">{title ? `${title} / ${map.label}` : map.label}</p>
        <address className="whitespace-pre-line not-italic text-ink-900">{map.value}</address>
        {map.url && (
          <a href={map.url} target="_blank" rel="noreferrer" className="inline-block text-sm font-semibold text-site-teal hover:underline">
            {contactPage.openMap}
          </a>
        )}
      </figcaption>
    </figure>
  );
}
