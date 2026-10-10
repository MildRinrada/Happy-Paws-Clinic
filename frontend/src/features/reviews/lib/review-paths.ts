/** Where the review pages live. One place, so a link and its page cannot drift apart. */
export const reviewPaths = {
  /** The public list. */
  list: '/reviews',
  /** The invitation page a one-time link opens. */
  invitation: (token: string) => `/review/${encodeURIComponent(token)}`,
  /** The signed-in owner's review page. */
  portal: '/portal/reviews',
  /** The staff moderation page. */
  staff: '/settings/reviews',
};
