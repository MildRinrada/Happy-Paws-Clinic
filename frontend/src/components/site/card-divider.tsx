/** A thin line across a card, between its body and its footer. */
export function CardDivider({ className = '' }: { className?: string }) {
  return <hr className={`border-0 border-t border-ink-200 ${className}`} />;
}
