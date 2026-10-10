import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import { getPrivacyPolicyVersion } from '@/features/privacy/api/getPrivacyPolicy';
import { OldVersionNotice } from '@/features/privacy/components/old-version-notice';
import { PrivacyDocument } from '@/features/privacy/components/privacy-document';
import { oldVersionNotice, privacyMeta } from '@/features/privacy/content/privacy-policy';
import { loadController } from '@/features/privacy/lib/load-controller';
import { privacyVersionHref } from '@/features/privacy/lib/privacy-paths';
import { pageMetadata } from '@/features/seo/lib/page-metadata';
import { optional } from '@/lib/api/api-error';

interface Props {
  params: Promise<{ version: string }>;
}

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { version } = await params;

  return {
    ...pageMetadata({
      title: oldVersionNotice.metaTitle(version),
      description: privacyMeta.description,
      path: privacyVersionHref(version),
    }),
    // The current notice is the one search engines should send people to.
    robots: { index: false, follow: true },
  };
}

/**
 * One published version of the privacy notice by its label, current or older
 * (docs/contract-extensions.md § 35), so the clinic can show a customer the exact text they agreed
 * under. An older one carries a banner pointing to the current one. A draft, or a label nobody
 * published, is the 404 page.
 */
export default async function PrivacyVersionPage({ params }: Props) {
  // Labels are letters, digits, dot, underscore and hyphen, so nothing in one needs decoding.
  const label = (await params).version;
  const [controller, policy] = await Promise.all([loadController(), optional(getPrivacyPolicyVersion(label))]);

  if (!policy?.published || !policy.label) {
    notFound();
  }

  return (
    <PrivacyDocument
      controller={controller}
      policy={policy}
      notice={policy.is_current ? undefined : <OldVersionNotice label={policy.label} />}
    />
  );
}
