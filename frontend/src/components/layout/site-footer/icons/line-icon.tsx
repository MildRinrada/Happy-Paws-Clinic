/** LINE's app icon: a white speech bubble on green, the name inside it. */
export function LineIcon({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className={className}>
      <rect width="48" height="48" rx="10" fill="#3ace01" />
      <ellipse cx="24" cy="22" rx="16" ry="12" fill="#ffffff" />
      <path d="M19 32l3 7 7-7z" fill="#ffffff" />
      <text
        x="24"
        y="25.5"
        textAnchor="middle"
        fontFamily="Arial, Helvetica, sans-serif"
        fontSize="9.5"
        fontWeight="700"
        fill="#3ace01"
      >
        LINE
      </text>
    </svg>
  );
}
