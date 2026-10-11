import { settingsNavText } from '../../content/settings-tabs';

/** The amber dot beside a page that still needs setting up, read out as words. */
export function AttentionDot() {
  return (
    <span className="inline-flex shrink-0 items-center">
      <span aria-hidden="true" className="size-2 rounded-full bg-amber-500" />
      <span className="sr-only">{settingsNavText.attention}</span>
    </span>
  );
}
