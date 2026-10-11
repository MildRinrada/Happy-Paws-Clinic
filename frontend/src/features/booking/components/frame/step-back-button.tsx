import type { ReactNode } from 'react';

/** "← ย้อนกลับ ...": the outlined button at the left of a step's foot. */
export function StepBackButton({ onClick, disabled, children }: { onClick: () => void; disabled?: boolean; children: ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="inline-flex h-[47px] items-center justify-center gap-2 rounded-[10px] border border-site-border bg-white px-5 text-[15px] font-semibold text-site-forest transition hover:bg-site-soft disabled:cursor-not-allowed disabled:opacity-50"
    >
      <span aria-hidden="true">←</span>
      {children}
    </button>
  );
}
