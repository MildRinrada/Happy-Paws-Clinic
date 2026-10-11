/**
 * The name the portal greets an owner by: the first word of the name the clinic has for them,
 * without a title in front. "สมใจ ใจดี" and "คุณสมใจ" are both "สมใจ", so the greeting reads
 * "สวัสดี คุณสมใจ" and never "คุณคุณสมใจ". The same rule as the backend's `ReviewNames.FirstNameOf`.
 *
 * Falls back to the whole name when taking the title off leaves nothing.
 */

/** Titles taken off the front, longest first so "นางสาว" is not read as "นาง". */
const THAI_TITLES = ['นางสาว', 'ด.ช.', 'ด.ญ.', 'น.ส.', 'นาง', 'นาย', 'คุณ', 'ดร.'];

const ENGLISH_TITLE = /^(mrs\.?|miss|mr\.?|ms\.?|dr\.?)\s+/i;

/** A Thai consonant, or a vowel written before its consonant (เ แ โ ใ ไ): what can begin a word. */
const STARTS_THAI_WORD = /^[ก-ฮเ-ไ]/;

function withoutTitle(name: string): string {
  for (const title of THAI_TITLES) {
    if (!name.startsWith(title)) continue;

    const rest = name.slice(title.length);
    // "คุณ สมใจ" or "คุณสมใจ", but "คุณากร" is a name: า cannot begin a word.
    if (rest.startsWith(' ') || STARTS_THAI_WORD.test(rest)) return rest.trimStart();
  }

  return name.replace(ENGLISH_TITLE, '');
}

export function firstNameOf(displayName: string): string {
  const name = displayName.trim().replace(/\s+/g, ' ');
  const first = withoutTitle(name).split(' ')[0];

  return first || name;
}
