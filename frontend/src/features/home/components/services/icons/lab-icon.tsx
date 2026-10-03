/** A blood sample: a capped test tube, part full. */
export function LabIcon({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className={className}>
      <rect x="18" y="2" width="12" height="7" rx="1.5" fill="#5b7fa6" />
      <rect x="16" y="8" width="16" height="4" rx="1.5" fill="#86a9cf" />
      <path d="M19 12h10v27a5 5 0 0 1-10 0z" fill="#e8eef5" />
      <path d="M19 24h10v15a5 5 0 0 1-10 0z" fill="#c8202f" />
    </svg>
  );
}
