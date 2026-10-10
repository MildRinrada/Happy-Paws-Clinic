'use client';

import { useState, type FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { FormStatus } from '@/components/ui/form-status';
import { formMessages } from '@/features/settings/content/form-messages';
import { useFormSubmit } from '@/hooks/use-form-submit';
import { updatePolicyDraftAction } from '../../actions/manage-privacy-policy-actions';
import { editorText, publishText } from '../../content/staff-privacy-policy';
import { sectionErrors } from '../../lib/section-errors';
import { blankSection, changed, moved, toDrafts, toSections, type SectionDraft } from '../../lib/section-draft';
import type { StaffPolicyVersion } from '../../types/staff-privacy-policy.types';
import { BodyFormatHelp } from './body-format-help';
import { DraftPreview } from './draft-preview';
import { PublishDialog } from './publish-dialog';
import { SectionEditorCard } from './section-editor-card';

interface DraftEditorProps {
  draft: StaffPolicyVersion;
  /** True for `settings:danger` holders, who see the publish form. */
  canPublish: boolean;
}

/**
 * A draft's sections: add, remove, reorder, edit, with the public page's rendering under them.
 * Saving sends every section and the version it was read at, so a colleague's save in between is
 * refused rather than overwritten.
 */
export function DraftEditor({ draft, canPublish }: DraftEditorProps) {
  const [stored, setStored] = useState(draft);
  const [sections, setSections] = useState<SectionDraft[]>(() => toDrafts(draft.sections));
  const { errors, message, saved, pending, submit, touch } = useFormSubmit(formMessages);
  const unsaved = changed(sections, stored.sections);

  function update(index: number, changes: Partial<SectionDraft>) {
    touch();
    setSections((current) => current.map((section, at) => (at === index ? { ...section, ...changes } : section)));
  }

  function replace(next: SectionDraft[]) {
    touch();
    setSections(next);
  }

  function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    submit(
      () => updatePolicyDraftAction({ id: stored.id, version: stored.version, sections: toSections(sections) }),
      (next) => {
        setStored(next);
        setSections(toDrafts(next.sections));
      },
    );
  }

  // A refusal names fields like `sections[2].body`; anything that names no section is said once at the bottom.
  const general = errors.sections ?? message ?? (Object.keys(errors).length > 0 ? editorText.sectionsError : undefined);

  return (
    <div className="space-y-6">
      <form onSubmit={save} noValidate className="space-y-4">
        <BodyFormatHelp />

        {sections.map((section, index) => (
          <SectionEditorCard
            key={section.key}
            section={section}
            index={index}
            count={sections.length}
            errors={sectionErrors(errors, index)}
            onChange={(changes) => update(index, changes)}
            onMove={(by) => replace(moved(sections, index, by))}
            onRemove={() => replace(sections.filter((_, at) => at !== index))}
          />
        ))}

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-ink-100 pt-4">
          <Button variant="secondary" onClick={() => replace([...sections, blankSection(sections, editorText.newHeading)])}>
            {editorText.add}
          </Button>
          <div className="flex flex-wrap items-center gap-3">
            {unsaved && !pending && <span className="text-sm text-amber-800">{editorText.unsaved}</span>}
            <FormStatus message={general} saved={saved} savedText={editorText.saved} />
            <Button type="submit" disabled={pending}>
              {pending ? editorText.saving : editorText.save}
            </Button>
          </div>
        </div>
      </form>

      <DraftPreview sections={toSections(sections)} />

      {canPublish ? (
        <PublishDialog draft={stored} unsaved={unsaved} />
      ) : (
        <p className="text-sm text-ink-600">{publishText.ownerOnly}</p>
      )}
    </div>
  );
}
