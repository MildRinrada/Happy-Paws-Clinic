/**
 * The refusals under one part of a request, with the part's prefix taken off: `new_item.name`
 * becomes `name`, `lines[1].quantity` (with prefix `lines[1].`) becomes `quantity`.
 */
export function errorsUnder(errors: Record<string, string>, prefix: string): Record<string, string> {
  return Object.fromEntries(
    Object.entries(errors)
      .filter(([field]) => field.startsWith(prefix))
      .map(([field, message]) => [field.slice(prefix.length), message]),
  );
}
