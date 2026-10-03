/** A first-aid mark: a red block with a white cross. */
export function SurgeryIcon({ className = '' }: { className?: string }) {
  return (
    <svg viewBox="0 0 48 48" aria-hidden="true" className={className}>
      <path d="M24 3l18 8v26a6 6 0 0 1-6 6H12a6 6 0 0 1-6-6V11z" fill="#f04438" />
      <path d="M21 17h6v7h7v6h-7v7h-6v-7h-7v-6h7z" fill="#ffffff" />
    </svg>
  );
}
