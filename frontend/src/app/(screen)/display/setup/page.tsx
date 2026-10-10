import type { Metadata } from 'next';
import { DisplaySetupForm } from '@/features/queue/components/display/display-setup-form';
import { queueDisplay } from '@/features/queue/content/queue-display';

export const metadata: Metadata = { title: queueDisplay.setup.metaTitle };

/**
 * Setting up a TV as a waiting-room screen (docs/contract-extensions.md § 30). Open to anyone: the
 * TV has no credential yet, and the setup code is what proves someone at the console made this
 * screen. Opened from the setup link, the code arrives after `#` and is used at once; typed, it
 * goes in the form.
 */
export default function DisplaySetupPage() {
  return <DisplaySetupForm />;
}
