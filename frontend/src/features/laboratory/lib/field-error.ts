/**
 * The first message for a field or any of its children. The backend names list items by index
 * (`tests[0].name`, `results[2].analyte`), so a list's errors are found by prefix.
 */
export function errorFor(errors: Record<string, string>, field: string): string | undefined {
  const key = Object.keys(errors).find((name) => name === field || name.startsWith(`${field}[`) || name.startsWith(`${field}.`));
  return key ? errors[key] : undefined;
}

/** Messages for fields a form does not draw, so a refusal is never silent. */
export function otherErrors(errors: Record<string, string>, shown: readonly string[]): string[] {
  return Object.entries(errors)
    .filter(([name]) => !shown.some((field) => name === field || name.startsWith(`${field}[`) || name.startsWith(`${field}.`)))
    .map(([, message]) => message);
}
