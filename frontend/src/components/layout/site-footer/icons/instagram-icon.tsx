/** Instagram's app icon: a camera outline on its gradient. */
export function InstagramIcon({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className={className}>
      <defs>
        <linearGradient id="footer-instagram" x1="0" y1="1" x2="1" y2="0">
          <stop offset="0" stopColor="#f1ac5f" />
          <stop offset="0.45" stopColor="#e44759" />
          <stop offset="0.75" stopColor="#c42f93" />
          <stop offset="1" stopColor="#7941b6" />
        </linearGradient>
      </defs>
      <rect width="48" height="48" rx="10" fill="url(#footer-instagram)" />
      <g fill="none" stroke="#ffffff" strokeWidth="2.6">
        <rect x="11" y="11" width="26" height="26" rx="7.5" />
        <circle cx="24" cy="24" r="6.2" />
      </g>
      <circle cx="31.4" cy="16.6" r="1.7" fill="#ffffff" />
    </svg>
  );
}
