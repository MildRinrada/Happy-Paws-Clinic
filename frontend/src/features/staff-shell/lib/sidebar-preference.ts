/**
 * Whether the sidebar is folded down to icons, remembered per browser. A cookie rather than local
 * storage, so the server draws the chosen width in the first HTML and the menu never jumps.
 * Not a credential: the worst a hand-edited value does is change the width.
 */
export const SIDEBAR_COOKIE = 'hp_sidebar';

const COLLAPSED = 'collapsed';

export function isCollapsed(value: string | undefined): boolean {
  return value === COLLAPSED;
}

/** Called from the browser. A blocked cookie only means the choice is not remembered next time. */
export function rememberCollapsed(collapsed: boolean): void {
  try {
    document.cookie = `${SIDEBAR_COOKIE}=${collapsed ? COLLAPSED : 'open'}; path=/; max-age=${60 * 60 * 24 * 365}; samesite=lax`;
  } catch {
    // Storage refused (privacy mode, a sandboxed frame): the sidebar still folds for this visit.
  }
}
