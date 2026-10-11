/** True on the page itself and on anything under it, such as one article's editor. */
export function isCurrentTab(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}
