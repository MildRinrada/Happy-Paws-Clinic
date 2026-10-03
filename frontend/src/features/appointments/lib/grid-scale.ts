/** Vertical scale of the day grid: 72 px an hour, enough for two lines in a 30-minute block. */
export const PX_PER_MINUTE = 1.2;

/** The top of something that starts at `minute`, inside a grid starting at `gridStart`. */
export function offsetPx(minute: number, gridStart: number): number {
  return (minute - gridStart) * PX_PER_MINUTE;
}

/** The whole hours from the grid's start to its end, for the axis and the hour lines. */
export function hoursBetween(start: number, end: number): number[] {
  const hours: number[] = [];
  for (let minute = start; minute < end; minute += 60) hours.push(minute);
  return hours;
}
