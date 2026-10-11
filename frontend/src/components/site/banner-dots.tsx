const dot = 'size-[26px] rounded-full';

/**
 * Four circles over a banner picture's bottom-right corner, the middle two raised: a decoration
 * only. Sits inside a `relative` box the size of the picture.
 */
export function BannerDots() {
  return (
    <div aria-hidden="true" className="absolute -bottom-[17px] -right-[13px] flex gap-2">
      <span className={`${dot} bg-site-sun`} />
      <span className={`${dot} -translate-y-[17px] bg-site-orange`} />
      <span className={`${dot} -translate-y-[17px] bg-site-teal`} />
      <span className={`${dot} bg-site-sun`} />
    </div>
  );
}
