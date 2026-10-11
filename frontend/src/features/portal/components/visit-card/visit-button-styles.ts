/** The card's two button looks: solid white for the main action, outlined for the rest. */
const base =
  'inline-flex h-11 min-w-0 items-center justify-center whitespace-nowrap rounded-xl px-3 font-display text-[13.5px] transition disabled:cursor-not-allowed disabled:opacity-70';

export const solidButton = `${base} bg-white text-brand-800 hover:bg-brand-50`;

export const outlineButton = `${base} border border-white/60 text-white hover:bg-white/10`;
