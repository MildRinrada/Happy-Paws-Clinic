'use server';

import { revalidatePath } from 'next/cache';
import { z } from 'zod';
import { runAction, type ActionResult } from '@/lib/actions/action-result';
import { createPolicyDraft, publishPolicy, updatePolicyDraft } from '../api/managePrivacyPolicy';
import { policySettingsPaths } from '../lib/privacy-paths';
import type { StaffPolicyVersion } from '../types/staff-privacy-policy.types';

/**
 * Drafting and publishing the clinic's privacy notice (docs/contract-extensions.md § 35). The
 * backend owns every rule: the body format, the label, the date, who may publish. These check the
 * shape, make the call, and mark the pages that show the notice stale, so a publish is on the
 * public page at once.
 */

const id = z.string().trim().min(1).max(40);
const version = z.number().int().nonnegative();

function refreshPolicyPages(versionId?: string) {
  revalidatePath('/privacy');
  revalidatePath('/privacy/versions/[version]', 'page');
  revalidatePath(policySettingsPaths.list);
  revalidatePath('/dashboard');

  if (versionId) {
    revalidatePath(policySettingsPaths.version(versionId));
  }
}

const source = z.discriminatedUnion('source', [
  z.object({ source: z.literal('template') }),
  z.object({ source: z.literal('current') }),
  z.object({ source: z.literal('version'), version_id: id }),
]);

export async function createPolicyDraftAction(values: z.infer<typeof source>): Promise<ActionResult<StaffPolicyVersion>> {
  const input = source.parse(values);
  const result = await runAction(() => createPolicyDraft(input));

  if (result.ok) refreshPolicyPages();
  return result;
}

const section = z.object({
  anchor: z.string().max(80),
  heading: z.string().max(300),
  body: z.string().max(20_000),
});

const edit = z.object({ id, version, sections: z.array(section).max(60) });

export async function updatePolicyDraftAction(values: z.infer<typeof edit>): Promise<ActionResult<StaffPolicyVersion>> {
  const input = edit.parse(values);
  const result = await runAction(() => updatePolicyDraft(input.id, input.sections, input.version));

  if (result.ok) refreshPolicyPages(input.id);
  return result;
}

const publish = z.object({
  id,
  version,
  label: z.string().max(64),
  effective_date: z.string().max(10),
  legal_review_done: z.boolean(),
});

export async function publishPolicyAction(values: z.infer<typeof publish>): Promise<ActionResult<StaffPolicyVersion>> {
  const { id: versionId, version: read, ...input } = publish.parse(values);
  const result = await runAction(() => publishPolicy(versionId, input, read));

  if (result.ok) refreshPolicyPages(versionId);
  return result;
}
