'use client';

import { Button } from '@/components/ui/button';
import { editorText } from '../../content/staff-privacy-policy';

interface SectionOrderButtonsProps {
  index: number;
  count: number;
  /** Read by a screen reader with each button, so "เลื่อนขึ้น" says which section. */
  heading: string;
  onMove: (by: -1 | 1) => void;
  onRemove: () => void;
}

/** Up, down and remove for one section. The last section cannot be removed: a notice needs one. */
export function SectionOrderButtons({ index, count, heading, onMove, onRemove }: SectionOrderButtonsProps) {
  return (
    <div className="flex flex-wrap items-center gap-1">
      <Button size="sm" variant="ghost" disabled={index === 0} aria-label={`${editorText.moveUp} ${heading}`} onClick={() => onMove(-1)}>
        {editorText.moveUp}
      </Button>
      <Button
        size="sm"
        variant="ghost"
        disabled={index === count - 1}
        aria-label={`${editorText.moveDown} ${heading}`}
        onClick={() => onMove(1)}
      >
        {editorText.moveDown}
      </Button>
      <Button size="sm" variant="danger" disabled={count === 1} aria-label={`${editorText.remove} ${heading}`} onClick={onRemove}>
        {editorText.remove}
      </Button>
    </div>
  );
}
