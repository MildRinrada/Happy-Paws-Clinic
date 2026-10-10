import { serializeJsonLd, type JsonLdObject } from '../lib/json-ld';

/** Structured data for search engines. Renders nothing visible. */
export function JsonLd({ data }: { data: JsonLdObject }) {
  // The content is escaped by serializeJsonLd, so API text cannot end the script early.
  return <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(data) }} />;
}
