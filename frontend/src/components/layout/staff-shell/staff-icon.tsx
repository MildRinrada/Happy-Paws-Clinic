import type { StaffIconName } from '@/features/staff-shell/types/staff-shell.types';

/**
 * The console's line icons, drawn in the same hand as the portal's (`portal-icon.tsx`): a 24 px
 * box, a 1.8 stroke in the current text colour, round ends. A dot is a zero-length stroke.
 */
const paths: Record<StaffIconName, string> = {
  dashboard: 'M5 4h5a1 1 0 0 1 1 1v6H4V5a1 1 0 0 1 1-1zM13 4h6a1 1 0 0 1 1 1v3h-7zM13 11h7v8a1 1 0 0 1-1 1h-6zM4 14h7v6H5a1 1 0 0 1-1-1z',
  queue: 'M4 7a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v3a2 2 0 0 0 0 4v3a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1v-3a2 2 0 0 0 0-4zM15 6v2M15 11v2M15 16v2',
  calendar: 'M7 3v3M17 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1z',
  messages: 'M5 5h14a1 1 0 0 1 1 1v10a1 1 0 0 1-1 1h-9l-4 3v-3H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1zM8 10h8M8 13h5',
  // A pad and four toes, the portal's paw.
  customers:
    'M12 13c-2.8 0-5 2.3-5 4.6C7 19.5 8.6 21 10.4 21c.6 0 1.1-.3 1.6-.3s1 .3 1.6.3c1.8 0 3.4-1.5 3.4-3.4 0-2.3-2.2-4.6-5-4.6z' +
    'M4.5 11.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0zM8 7.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0z' +
    'M12 7.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0zM15.5 11.5a2 2.5 0 1 0 4 0a2 2.5 0 1 0-4 0z',
  lab: 'M9 3h6M10 3v6l-5 9a2 2 0 0 0 1.7 3h10.6a2 2 0 0 0 1.7-3l-5-9V3M7.5 15h9',
  prescriptions: 'M4.8 13.2l8.4-8.4a4 4 0 0 1 5.7 5.7l-8.4 8.4a4 4 0 0 1-5.7-5.7zM9 9l6 6',
  billing: 'M6 3h12v18l-3-2-3 2-3-2-3 2zM9 8h6M9 12h6M9 16h3',
  inventory: 'M3 7.5 12 3l9 4.5v9L12 21l-9-4.5zM3 7.5l9 4.5 9-4.5M12 12v9',
  crm: 'M3 12V4a1 1 0 0 1 1-1h8l9 9-9 9zM8 8h.01',
  notifications: 'M6 16V11a6 6 0 1 1 12 0v5l2 2H4zM10 20a2 2 0 0 0 4 0',
  schedules: 'M9 3h6v3H9zM9 4.5H6a1 1 0 0 0-1 1V20a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V5.5a1 1 0 0 0-1-1h-3M8 11h8M8 15h5',
  reports: 'M4 20h16M7 16v-5M12 16V7M17 16v-8',
  ai: 'M11 3l1.8 5.2L18 10l-5.2 1.8L11 17l-1.8-5.2L4 10l5.2-1.8zM18 14.5l.8 2.2 2.2.8-2.2.8L18 20.5l-.8-2.2-2.2-.8 2.2-.8z',
  settings: 'M4 7h9M17 7h3M4 17h3M11 17h9M13 7a2 2 0 1 0 4 0a2 2 0 1 0-4 0zM7 17a2 2 0 1 0 4 0a2 2 0 1 0-4 0z',
  menu: 'M4 6h16M4 12h16M4 18h16',
  close: 'M6 6l12 12M18 6 6 18',
  search: 'M11 4a7 7 0 1 0 0 14a7 7 0 1 0 0-14zM20 20l-4-4',
  plus: 'M12 5v14M5 12h14',
  chevronDown: 'M6 9l6 6 6-6',
  collapse: 'M5 4h14a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1zM9 4v16M16 9.5 13.5 12l2.5 2.5',
  expand: 'M5 4h14a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1zM9 4v16M13.5 9.5 16 12l-2.5 2.5',
  account: 'M12 4a4 4 0 1 0 0 8a4 4 0 1 0 0-8zM4.5 20a7.5 7.5 0 0 1 15 0',
  signOut: 'M14 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4M10 8l-4 4 4 4M6 12h10',
};

/** One of the console's icons, hidden from screen readers: the text beside it says what it is. */
export function StaffIcon({ name, className = 'size-5' }: { name: StaffIconName; className?: string }) {
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
