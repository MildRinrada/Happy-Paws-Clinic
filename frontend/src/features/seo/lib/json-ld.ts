/** A schema.org object, as JSON-LD wants it. */
export type JsonLdObject = Record<string, unknown>;

/**
 * JSON for a `<script type="application/ld+json">`, safe to put in the page as raw HTML.
 *
 * Text from the API (an article title, a service name) must not be able to close the script tag
 * and start HTML of its own, so every `<` and `>` is written as a JSON escape. `&` and the two
 * line separators JavaScript treats as line ends are escaped too. The JSON means the same thing.
 */
export function serializeJsonLd(data: JsonLdObject): string {
  return JSON.stringify(data)
    .replace(/</g, '\\u003c')
    .replace(/>/g, '\\u003e')
    .replace(/&/g, '\\u0026')
    .replace(/\u2028/g, '\\u2028')
    .replace(/\u2029/g, '\\u2029');
}

/** Drops keys whose value is `undefined`, an empty string or an empty list, so the output holds only what is known. */
export function compact<T extends JsonLdObject>(data: T): T {
  return Object.fromEntries(
    Object.entries(data).filter(
      ([, value]) => value !== undefined && value !== '' && !(Array.isArray(value) && value.length === 0),
    ),
  ) as T;
}
