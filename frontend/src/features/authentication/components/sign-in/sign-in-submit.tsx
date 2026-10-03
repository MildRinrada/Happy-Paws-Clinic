import type { ReactNode } from 'react';

/** The wide orange button that sends the form. */
export function SignInSubmit({ disabled, children }: { disabled?: boolean; children: ReactNode }) {
  return (
    <button
      type="submit"
      disabled={disabled}
      className="h-12 w-full rounded-[10px] bg-site-orange text-[15px] font-bold text-white transition hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-70"
    >
      {children}
    </button>
  );
}
