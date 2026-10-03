/** The card's two button looks: solid white for the main action, outlined for the rest. */
const base =
  'inline-flex h-11 flex-1 items-center justify-center whitespace-nowrap rounded-xl px-3 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-70';

export const solidButton = `${base} bg-white text-site-teal hover:bg-white/90`;

export const outlineButton = `${base} border border-white/70 text-white hover:bg-white/10`;
