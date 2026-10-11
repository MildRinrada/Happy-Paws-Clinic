import type { ReactNode } from 'react';

interface StepNextButtonProps {
  children: ReactNode;
  onClick?: () => void;
  /** `submit` when the button sends the step's form. */
  type?: 'button' | 'submit';
  disabled?: boolean;
  /** The "→" after the words. Left off on the summary card's full-width button. */
  arrow?: boolean;
  /** Full width, as on the summary card and a step with no way back. */
  block?: boolean;
}

/** The orange button with dark words that moves the booking on. */
export function StepNextButton({ children, onClick, type = 'button', disabled, arrow = true, block = false }: StepNextButtonProps) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`inline-flex min-h-[47px] items-center justify-center gap-2 rounded-[10px] bg-site-orange px-6 py-2 text-[15px] font-bold text-ink-900 transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-50 ${
        block ? 'w-full' : ''
      }`}
    >
      {children}
      {arrow && <span aria-hidden="true">→</span>}
    </button>
  );
}
