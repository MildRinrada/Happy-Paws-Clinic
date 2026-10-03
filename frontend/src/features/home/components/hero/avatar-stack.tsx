const circle = 'size-[38px] rounded-full ring-2 ring-site-teal';

/** Overlapping circles standing for the clinic's patients, the last one carrying the count. */
export function AvatarStack({ badge }: { badge: string }) {
  return (
    <div aria-hidden="true" className="flex shrink-0 -space-x-3.5">
      <span className={`${circle} bg-[#ffff00]`} />
      <span className={`${circle} bg-site-orange`} />
      <span className={`${circle} bg-[#c77df3]`} />
      <span className={`${circle} flex items-center justify-center bg-black text-xs font-medium text-white`}>
        {badge}
      </span>
    </div>
  );
}
