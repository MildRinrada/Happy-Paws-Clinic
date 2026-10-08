/** The look-ahead windows offered for "ใกล้หมดอายุ", in days. The backend accepts 1 to 365. */
export const ALERT_WINDOWS = [30, 60, 90, 180];

export const DEFAULT_ALERT_WINDOW = 90;

/** The window from the address, or the default when it is not one of the offered ones. */
export function readAlertWindow(value: string | string[] | undefined): number {
  const days = Number(Array.isArray(value) ? value[0] : value);

  return ALERT_WINDOWS.includes(days) ? days : DEFAULT_ALERT_WINDOW;
}
