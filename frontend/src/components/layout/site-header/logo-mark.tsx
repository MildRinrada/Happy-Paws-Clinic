/**
 * A stand-in for the clinic's logo: a paw whose pad is a small clinic with a cross on it.
 *
 * Drawn here so the header has something in the logo's place. When the real logo arrives, put
 * the file in /public and replace this drawing in `site-logo.tsx`; nothing else refers to it.
 */
export function LogoMark({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 64 64" aria-hidden="true" className={className}>
      <g fill="#4b5158">
        <ellipse cx="9.5" cy="27" rx="5.6" ry="7.6" transform="rotate(-28 9.5 27)" />
        <ellipse cx="23" cy="13.5" rx="6" ry="8.4" transform="rotate(-8 23 13.5)" />
        <ellipse cx="41" cy="13.5" rx="6" ry="8.4" transform="rotate(8 41 13.5)" />
        <ellipse cx="54.5" cy="27" rx="5.6" ry="7.6" transform="rotate(28 54.5 27)" />
      </g>
      <path
        d="M32 27 50 41v17a3 3 0 0 1-3 3H17a3 3 0 0 1-3-3V41z"
        fill="#2aa8e0"
        stroke="#2aa8e0"
        strokeWidth="3"
        strokeLinejoin="round"
      />
      <path d="M29 39h6v6h6v6h-6v6h-6v-6h-6v-6h6z" fill="#ffffff" />
    </svg>
  );
}
