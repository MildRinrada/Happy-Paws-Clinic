import type { PetListHint } from '../../../lib/pet-list-items';

const looks: Record<PetListHint['tone'], { dot: string; text: string }> = {
  alert: { dot: 'bg-red-700', text: 'text-red-800' },
  warn: { dot: 'bg-amber-500', text: 'text-amber-900' },
};

/** "วัคซีนพิษสุนัขบ้าเลยกำหนด" with a red dot, or a yellow one when it is only due soon. */
export function PetStatusHint({ hint }: { hint: PetListHint }) {
  const look = looks[hint.tone];

  return (
    <p className={`flex items-center gap-1.5 text-[12.5px] leading-5 ${look.text}`}>
      <span aria-hidden="true" className={`size-2 shrink-0 rounded-full ${look.dot}`} />
      <span className="truncate">{hint.text}</span>
    </p>
  );
}
