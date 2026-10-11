interface LiveHeaderProps {
  title: string;
  /** Draws the pulsing dot. Only for a visit happening today. */
  live: boolean;
  badge?: string;
}

/** The card's top line: what it is following, and the appointment's state as a pill. */
export function LiveHeader({ title, live, badge }: LiveHeaderProps) {
  return (
    <div className="flex items-center justify-between gap-3">
      <h2 className="flex min-w-0 items-center gap-2.5 font-display text-base font-semibold leading-6 text-white">
        {live && (
          <span className="relative flex size-2 shrink-0" aria-hidden="true">
            <span className="absolute inline-flex size-full animate-ping rounded-full bg-brand-300 opacity-75 motion-reduce:hidden" />
            <span className="relative inline-flex size-2 rounded-full bg-brand-300" />
          </span>
        )}
        {title}
      </h2>

      {badge && (
        <span className="shrink-0 rounded-full bg-brand-100 px-2.5 text-xs font-medium leading-[23px] text-brand-800">
          {badge}
        </span>
      )}
    </div>
  );
}
