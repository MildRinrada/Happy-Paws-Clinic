/**
 * The worklist's clock: when a waiting request counts as late, and how often the page reads itself
 * again. Clinic settings in code, kept here so changing one is a one-line edit.
 */
export const labTiming = {
  /** An in-house sample still without results after this long is drawn as late. */
  inHouseOverdueMinutes: 2 * 60,
  /** A sample sent out still without results after this long is drawn as late. */
  externalOverdueMinutes: 48 * 60,
  /** The worklist reads itself again this often while it is on screen. */
  refreshSeconds: 60,
};
