import type { ContactGroup } from '../lib/group-contacts';
import { ContactCard } from './contact-card';
import { MapPanel } from './map-panel';

/** One group of entries: the clinic's own, or one branch's. Cards first, then its maps. */
export function ContactGroupSection({ group }: { group: ContactGroup }) {
  return (
    <section aria-label={group.title} className="space-y-4">
      {group.title && <h3 className="text-lg font-bold text-ink-900">{group.title}</h3>}

      {group.channels.length > 0 && (
        // Two across at most: the column beside the hours is narrow, and a phone number or email
        // must stay on one line to be read and copied.
        <ul className="grid gap-4 sm:grid-cols-2">
          {group.channels.map((contact) => (
            <ContactCard key={contact.id} contact={contact} />
          ))}
        </ul>
      )}

      {group.maps.length > 0 && (
        <div className="grid gap-4">
          {group.maps.map((map) => (
            <MapPanel key={map.id} map={map} />
          ))}
        </div>
      )}
    </section>
  );
}
