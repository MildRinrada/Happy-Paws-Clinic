const paths = {
  down: 'm5 8 5 5 5-5',
  left: 'm12.5 4.5-5.5 5.5 5.5 5.5',
  right: 'm7.5 4.5 5.5 5.5-5.5 5.5',
};

/** A small arrow head in the current text colour: a list that opens, the way back, the way in. */
export function ChevronIcon({ direction, className = 'size-4' }: { direction: keyof typeof paths; className?: string }) {
  return (
    <svg
      viewBox="0 0 20 20"
      aria-hidden="true"
      className={className}
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d={paths[direction]} />
    </svg>
  );
}
