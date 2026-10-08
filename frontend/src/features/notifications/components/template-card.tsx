import { Badge } from '@/components/ui/badge';
import { Card, CardBody, CardHeader } from '@/components/ui/card';
import { variableLabels } from '../content/send-page';
import { templatesPage } from '../content/templates-page';
import type { NotificationTemplate } from '../types/notification.types';

/** One template: who sends it, the consent it needs, its values, and its words per channel. */
export function TemplateCard({ template }: { template: NotificationTemplate }) {
  const text = templatesPage;

  return (
    <Card>
      <CardHeader
        title={template.name}
        aside={
          <span className="flex flex-wrap gap-1.5">
            <Badge tone={template.staff_sendable ? 'brand' : 'neutral'}>{template.staff_sendable ? text.staffSendable : text.systemOnly}</Badge>
            {!template.line_enabled && <Badge tone="amber">{text.lineOff}</Badge>}
          </span>
        }
      />
      <CardBody className="space-y-3 text-sm">
        <p className="flex flex-wrap gap-x-3 gap-y-1 text-ink-600">
          <span className="font-mono text-xs text-ink-500">{template.key}</span>
          <span>{text.purposes[template.purpose] ?? template.purpose}</span>
        </p>

        {template.variables.length > 0 && (
          <p className="text-ink-700">
            <span className="font-medium">{text.variables}: </span>
            {template.variables
              .map((variable) => `${variableLabels[variable.name]?.label ?? variable.name}${variable.required ? '' : ` (${text.optional})`}`)
              .join(', ')}
          </p>
        )}

        <dl className="grid gap-3 sm:grid-cols-2">
          <div className="rounded-lg bg-ink-50 px-3 py-2">
            <dt className="text-xs font-medium text-ink-500">{text.bodies.line}</dt>
            <dd className="mt-1 whitespace-pre-wrap text-ink-800">{template.bodies.line}</dd>
          </div>
          <div className="rounded-lg bg-ink-50 px-3 py-2">
            <dt className="text-xs font-medium text-ink-500">
              {text.bodies.sms} <span className="font-normal">({text.smsLength(template.bodies.sms.length)})</span>
            </dt>
            <dd className="mt-1 whitespace-pre-wrap text-ink-800">{template.bodies.sms}</dd>
          </div>
        </dl>
      </CardBody>
    </Card>
  );
}
