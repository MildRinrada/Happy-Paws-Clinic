/** Facebook's app icon: a white "f" on blue. */
export function FacebookIcon({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className={className}>
      <rect width="48" height="48" rx="10" fill="#1877f2" />
      <path
        d="M33 48V30h5.5l1-7H33v-4.2c0-2.2.9-3.8 3.9-3.8H40V8.6c-.7-.1-2.8-.3-5.2-.3-5.3 0-8.8 3.2-8.8 9.1V23h-6v7h6v18z"
        fill="#ffffff"
      />
    </svg>
  );
}
