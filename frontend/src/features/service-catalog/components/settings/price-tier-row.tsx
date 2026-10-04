import { Button } from '@/components/ui/button';
import { Input, Select } from '@/components/ui/field';
import { registerPet } from '@/features/pets/content/register-pet';
import { serviceSettings } from '../../content/service-settings';
import type { TierRow } from '../../lib/tier-rows';

interface PriceTierRowProps {
  row: TierRow;
  error?: string;
  onChange: (row: TierRow) => void;
  onRemove: () => void;
}

/** One line of a price list: a kind of animal, a weight band, or both, and its price. */
export function PriceTierRow({ row, error, onChange, onRemove }: PriceTierRowProps) {
  const text = serviceSettings.pricing;
  const species = registerPet.species.includes(row.species) || row.species === '' ? registerPet.species : [...registerPet.species, row.species];

  return (
    <li className="space-y-1">
      <div className="flex flex-wrap items-center gap-2">
        <Select aria-label={text.species} value={row.species} className="max-w-36" onChange={(event) => onChange({ ...row, species: event.target.value })}>
          <option value="">{text.anySpecies}</option>
          {species.map((name) => (
            <option key={name} value={name}>
              {name}
            </option>
          ))}
        </Select>
        <Input aria-label={text.minKg} placeholder={text.minKg} inputMode="decimal" value={row.minKg} className="max-w-28" onChange={(event) => onChange({ ...row, minKg: event.target.value })} />
        <Input aria-label={text.maxKg} placeholder={text.maxKg} inputMode="decimal" value={row.maxKg} className="max-w-28" onChange={(event) => onChange({ ...row, maxKg: event.target.value })} />
        <Input aria-label={text.tierPrice} placeholder={text.tierPrice} inputMode="decimal" value={row.price} required className="max-w-32" onChange={(event) => onChange({ ...row, price: event.target.value })} />
        <Button variant="ghost" size="sm" onClick={onRemove}>
          {text.removeTier}
        </Button>
      </div>
      {error && (
        <p role="alert" className="text-sm text-red-700">
          {error}
        </p>
      )}
    </li>
  );
}
