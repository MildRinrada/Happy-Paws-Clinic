/**
 * The authenticator code as people type it. Apps show it in two groups ("162 544"), and copying
 * from the screen brings the space along, but the backend wants the digits alone.
 */

/** Digits with spaces between groups: six to eight digits, so at most eleven characters. */
export const ONE_TIME_CODE_PATTERN = '[0-9 ]{6,11}';
export const ONE_TIME_CODE_MAX_LENGTH = 11;

/** Drops every space, so "162 544" reaches the backend as "162544". */
export function oneTimeCodeDigits(code: string): string {
  return code.replace(/\s+/g, '');
}
