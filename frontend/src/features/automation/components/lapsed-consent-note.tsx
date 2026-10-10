import { automationsPage } from '../content/automations-page';

/**
 * Under a "คิดถึงน้อง" rule: who it reaches, and that it goes out under marketing consent, so a
 * customer who never agreed to news is skipped even though they get care reminders
 * (docs/contract-extensions.md § 33).
 */
export function LapsedConsentNote() {
  const text = automationsPage.lapsed;

  return (
    <div className="space-y-1 rounded-lg border border-amber-200 bg-amber-50 px-3.5 py-2.5 text-sm text-amber-900">
      <p className="font-medium">{text.consentTitle}</p>
      <p>{text.consentBody}</p>
      <p className="text-amber-800">{text.ruleBody}</p>
    </div>
  );
}
