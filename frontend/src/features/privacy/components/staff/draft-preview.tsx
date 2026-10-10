import { previewText } from '../../content/staff-privacy-policy';
import type { PolicySection as Section } from '../../types/privacy-policy.types';
import { PolicyBody } from '../policy-body';
import { PolicySection } from '../policy-section';

/**
 * The notice as the public page will draw it, from what is typed now. Same `PolicyBody` and
 * `PolicySection` as `/privacy`: text nodes and site links only, so markup shows as typed.
 */
export function DraftPreview({ sections }: { sections: Section[] }) {
  return (
    <section aria-label={previewText.heading} className="space-y-6 rounded-xl border border-ink-200 bg-white p-5">
      <div>
        <h2 className="font-display text-base font-semibold text-ink-900">{previewText.heading}</h2>
        <p className="mt-1 text-xs text-ink-500">{previewText.note}</p>
      </div>
      <div className="max-w-[720px] space-y-10">
        {sections.map((section, index) => (
          <PolicySection key={`${section.anchor}-${index}`} id={`preview-${index}`} heading={section.heading || '-'}>
            {section.body.trim() ? <PolicyBody body={section.body} /> : <p className="text-sm text-ink-500">{previewText.empty}</p>}
          </PolicySection>
        ))}
      </div>
    </section>
  );
}
