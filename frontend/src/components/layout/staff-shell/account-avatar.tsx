/** The signed-in person's initials in a mint circle. */
export function AccountAvatar({ initials, className = 'size-8 text-sm' }: { initials: string; className?: string }) {
  return (
    <span
      aria-hidden
      className={`flex shrink-0 items-center justify-center rounded-full bg-brand-100 font-semibold text-brand-800 ${className}`}
    >
      {initials}
    </span>
  );
}
