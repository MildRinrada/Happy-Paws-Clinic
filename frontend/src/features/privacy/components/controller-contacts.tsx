import type { ControllerContact } from '../lib/load-controller';

/** The clinic's phone numbers and email, each a call or mail link, as set in the staff console. */
export function ControllerContacts({ contacts }: { contacts: ControllerContact[] }) {
  return (
    <ul className="space-y-1 text-[15px] text-ink-700">
      {contacts.map((contact) => (
        <li key={`${contact.label}-${contact.value}`} className="break-words">
          <span className="text-site-muted">{contact.label}</span>{' '}
          {contact.href ? (
            <a href={contact.href} className="font-semibold text-site-teal hover:underline">
              {contact.value}
            </a>
          ) : (
            <span className="font-semibold text-ink-900">{contact.value}</span>
          )}
        </li>
      ))}
    </ul>
  );
}
