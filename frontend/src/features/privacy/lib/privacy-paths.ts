/** Where a published version of the notice is read, current or older (docs/contract-extensions.md § 35). */
export function privacyVersionHref(label: string): string {
  return `/privacy/versions/${encodeURIComponent(label)}`;
}

/** The console's pages for the notice. */
export const policySettingsPaths = {
  list: '/settings/privacy-policy',
  version: (id: string) => `/settings/privacy-policy/${encodeURIComponent(id)}`,
};
