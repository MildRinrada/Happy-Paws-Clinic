import type { QuickActionIcon as IconName } from '../../content/portal-home';

const paths: Record<IconName, string> = {
  // A calendar with a plus on it.
  book: 'M7 3v3M17 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1zM12 12v5M9.5 14.5h5',
  // A page with lines of writing.
  bills: 'M7 3h7l5 5v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1zM14 3v5h5M9 13h6M9 17h6',
  // A wrapped box with a bow.
  packages:
    'M4 8h16v4H4zM5 12h14v8a1 1 0 0 1-1 1H6a1 1 0 0 1-1-1zM12 8v13' +
    'M12 8c-1.5-3-5-3.5-5-1.2C7 8 9 8 12 8zM12 8c1.5-3 5-3.5 5-1.2C17 8 15 8 12 8z',
};

/** The line drawing on a shortcut tile, in the current text colour. */
export function QuickActionIcon({ name }: { name: IconName }) {
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden="true"
      className="size-6"
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
