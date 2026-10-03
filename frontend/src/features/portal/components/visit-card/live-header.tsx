interface LiveHeaderProps {
  title: string;
  /** Draws the pulsing green dot. Only for a visit happening today. */
  live: boolean;
  badge?: string;
}

/** The card's top line: what it is following, and the appointment's state as a pill. */
export function LiveHeader({ title, live, badge }: LiveHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3">
      <p className="flex items-center gap-2 text-sm font-semibold text-white">
        {live && (
          <span className="relative flex size-2.5" aria-hidden="true">
            <span className="absolute inline-flex size-full animate-ping rounded-full bg-green-300 opacity-75 motion-reduce:hidden" />
            <span className="relative inline-flex size-2.5 rounded-full bg-green-400" />
          </span>
        )}
        {title}
      </p>

      {badge && <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-semibold text-white">{badge}</span>}
    </div>
  );
}
