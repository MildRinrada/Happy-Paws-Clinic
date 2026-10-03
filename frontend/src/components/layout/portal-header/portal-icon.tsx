import type { PortalIcon as IconName } from '@/features/portal/content/portal-navigation';

const paths: Record<IconName | 'bell', string> = {
  home: 'M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6h-6v6H4a1 1 0 0 1-1-1z',
  // A pad and four toes, each toe a full ellipse drawn as two arcs.
  paw:
    'M12 13c-2.8 0-5 2.3-5 4.6C7 19.5 8.6 21 10.4 21c.6 0 1.1-.3 1.6-.3s1 .3 1.6.3c1.8 0 3.4-1.5 3.4-3.4 0-2.3-2.2-4.6-5-4.6z' +
    'M4.5 11.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0zM8 7.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0z' +
    'M12 7.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0zM15.5 11.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0z',
  calendar: 'M7 3v3M17 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1z',
  records: 'M7 3h7l5 5v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1zM14 3v5h5M9 13h6M9 17h6',
  bell: 'M6 16V11a6 6 0 1 1 12 0v5l2 2H4zM10 20a2 2 0 0 0 4 0',
};

/** The portal's line icons, drawn in the current text colour. */
export function PortalIcon({ name, className = 'size-6' }: { name: IconName | 'bell'; className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className={className}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d={paths[name]} />
    </svg>
  );
}
