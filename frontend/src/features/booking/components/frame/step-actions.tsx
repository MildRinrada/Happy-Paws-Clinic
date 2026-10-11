import type { ReactNode } from 'react';

/**
 * The foot of a step's card: a line, "ย้อนกลับ" at the left and the next step's button at the
 * right. On a phone the buttons stack, the next one first.
 */
export function StepActions({ back, next }: { back?: ReactNode; next: ReactNode }) {
  return (
    <div
      className={`mt-7 flex flex-col-reverse gap-3 border-t border-ink-200 pt-6 sm:flex-row sm:items-center ${
        back ? 'sm:justify-between' : 'sm:justify-end'
      }`}
    >
      {back}
      {next}
    </div>
  );
}
