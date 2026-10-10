import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { CopyValue } from '@/features/integrations/components/copy-value';
import { copyLabels } from '@/features/integrations/content/copy';
import { formatTime } from '@/lib/format/datetime';
import { queueDisplays } from '../../content/queue-displays';
import type { DisplaySetup } from '../../types/queue.types';

/**
 * A screen just made: the setup code to type on the TV, the setup link and its QR code, shown this
 * once. None of them is the screen's token: the TV trades the code for that itself, and it never
 * appears on this page.
 */
export function CreatedDisplayCard({ setup, onDone }: { setup: DisplaySetup; onDone: () => void }) {
  const text = queueDisplays.created;

  return (
    <Card className="border-amber-300">
      <CardHeader title={text.heading(setup.display.name)} />
      <CardBody className="space-y-4">
        <Alert tone="error">{text.warning}</Alert>

        <div className="flex flex-wrap items-start gap-6">
          <div className="min-w-0 flex-1 space-y-4">
            <p>
              <span className="block text-sm text-ink-500">{text.code}</span>
              <span className="font-display text-4xl font-semibold tracking-widest text-ink-900">{setup.display.setup_code}</span>
              <span className="block text-sm text-ink-600">{text.until(formatTime(setup.display.setup_expires_at))}</span>
            </p>
            <CopyValue label={text.link} value={setup.setupUrl} copyLabel={copyLabels.copy} copiedLabel={copyLabels.copied} />
            <ol className="list-decimal space-y-1 pl-5 text-sm text-ink-700">
              {text.steps.map((step) => (
                <li key={step}>{step}</li>
              ))}
            </ol>
          </div>

          {/* A data URL drawn on this app's server, so the link never leaves for an image service. */}
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={setup.qrDataUrl} alt={text.qr} width={192} height={192} className="rounded-lg border border-ink-200" />
        </div>

        <Button variant="ghost" size="sm" onClick={onDone}>
          {text.done}
        </Button>
      </CardBody>
    </Card>
  );
}
