/** A shower head with water coming out of it. */
export function GroomingIcon({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className={className}>
      <path d="M44 44V18a12 12 0 0 0-22-7" fill="none" stroke="#9aa5ae" strokeWidth="5" strokeLinecap="round" />
      <path d="M10 22 27 5l6 6-17 17z" fill="#b9c2c9" />
      <g stroke="#3aa7e8" strokeWidth="3" strokeLinecap="round">
        <path d="M13 29l-3 3M19 31l-3 3M8 35l-3 3M14 37l-3 3M21 37l-3 3M10 43l-2 2M17 43l-2 2" />
      </g>
    </svg>
  );
}
