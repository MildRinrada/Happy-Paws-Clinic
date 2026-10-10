type SectionField = 'heading' | 'anchor' | 'body';

const fields: SectionField[] = ['heading', 'anchor', 'body'];

/** One section's messages out of a refusal keyed by the API's field paths, `sections[2].body`. */
export function sectionErrors(errors: Record<string, string>, index: number): Partial<Record<SectionField, string>> {
  const found: Partial<Record<SectionField, string>> = {};

  for (const field of fields) {
    const message = errors[`sections[${index}].${field}`];
    if (message) found[field] = message;
  }

  return found;
}
