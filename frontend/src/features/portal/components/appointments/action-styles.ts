/** The small buttons under an appointment: outlined for moving it, red-outlined for cancelling. */
const base =
  'inline-flex h-9 items-center justify-center whitespace-nowrap rounded-xl px-3 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-60';

export const plainAction = `${base} border border-ink-200 text-ink-700 hover:bg-ink-50`;

export const dangerAction = `${base} border border-red-200 text-red-700 hover:bg-red-50`;

export const dangerSolid = `${base} bg-red-600 text-white hover:bg-red-700`;
