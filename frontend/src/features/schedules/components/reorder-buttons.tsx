import { assignmentRulesText } from '../content/assignment-rules';

interface ReorderButtonsProps {
  /** What is being moved, for screen readers: "ข้อ 2", a person's name. */
  label: string;
  isFirst: boolean;
  isLast: boolean;
  onMove: (step: -1 | 1) => void;
  onRemove: () => void;
  removeLabel?: string;
  disabled?: boolean;
}

const button =
  'inline-flex size-7 items-center justify-center rounded-md text-ink-500 hover:bg-ink-100 hover:text-ink-800 disabled:pointer-events-none disabled:opacity-30';

/** Up, down and remove for one item in an ordered list, where the order is the meaning. */
export function ReorderButtons({ label, isFirst, isLast, onMove, onRemove, removeLabel, disabled }: ReorderButtonsProps) {
  const text = assignmentRulesText;

  return (
    <span className="inline-flex shrink-0 items-center">
      <button type="button" className={button} disabled={disabled || isFirst} aria-label={`${text.moveUp} ${label}`} onClick={() => onMove(-1)}>
        <span aria-hidden="true">↑</span>
      </button>
      <button type="button" className={button} disabled={disabled || isLast} aria-label={`${text.moveDown} ${label}`} onClick={() => onMove(1)}>
        <span aria-hidden="true">↓</span>
      </button>
      <button
        type="button"
        className={`${button} hover:text-red-700`}
        disabled={disabled}
        aria-label={`${removeLabel ?? text.remove} ${label}`}
        onClick={onRemove}
      >
        <span aria-hidden="true">×</span>
      </button>
    </span>
  );
}
