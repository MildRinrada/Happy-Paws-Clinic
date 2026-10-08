import { Field, Input, Textarea } from '@/components/ui/field';
import { variableLabels, type SendableTemplate } from '../content/send-page';

interface VariableFieldsProps {
  template: SendableTemplate;
  values: Record<string, string>;
  errors: Record<string, string>;
  onChange: (name: string, value: string) => void;
}

/** One field per value the chosen template fills in. Errors arrive keyed `variables.<name>`. */
export function VariableFields({ template, values, errors, onChange }: VariableFieldsProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {template.variables.map((variable) => {
        const id = `variable-${variable.name}`;
        const label = variableLabels[variable.name];
        const common = {
          id,
          value: values[variable.name] ?? '',
          maxLength: variable.maxLength,
          required: variable.required,
          placeholder: label?.placeholder,
          'aria-invalid': errors[`variables.${variable.name}`] ? true : undefined,
        };

        return (
          <div key={variable.name} className={variable.multiline ? 'sm:col-span-2' : undefined}>
            <Field label={label?.label ?? variable.name} htmlFor={id} error={errors[`variables.${variable.name}`]}>
              {variable.multiline ? (
                <Textarea {...common} rows={4} onChange={(event) => onChange(variable.name, event.target.value)} />
              ) : (
                <Input {...common} onChange={(event) => onChange(variable.name, event.target.value)} />
              )}
            </Field>
          </div>
        );
      })}
    </div>
  );
}
