/**
 * The clinic's mark as every page draws it (docs/contract-extensions.md § 50). Without `logoSrc`
 * the pages draw the stand-in paw, exactly as they did before a logo could be set.
 */
export interface ClinicIdentity {
  /** The clinic's name: the logo's alternative text when the name is not written beside it. */
  name: string;
  /** Where the browser loads the logo from. */
  logoSrc?: string;
  /** Where the browser loads the optional square tab icon from. */
  tabIconSrc?: string;
  /** Whether the name is written beside the logo. Only matters while there is a logo. */
  showName: boolean;
}
