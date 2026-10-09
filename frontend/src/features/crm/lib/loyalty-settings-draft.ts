import { parseBahtToSatang } from '@/lib/format/money';
import type { LoyaltySettings, LoyaltySettingsInput } from '../types/crm.types';

/** The form's text, as typed. Money in baht, as people think of it; the API takes satang. */
export interface LoyaltySettingsDraft {
  bahtPerPoint: string;
  pointValueBaht: string;
  silver: string;
  gold: string;
  months: string;
}

export type LoyaltySettingsField = keyof LoyaltySettingsDraft;

const baht = (satang: number) => String(satang / 100);

export function draftFromSettings(settings: LoyaltySettings): LoyaltySettingsDraft {
  return {
    bahtPerPoint: baht(settings.satang_per_point),
    pointValueBaht: baht(settings.point_value_satang),
    silver: String(settings.silver_min_points),
    gold: String(settings.gold_min_points),
    months: settings.points_expire_after_months ? String(settings.points_expire_after_months) : '',
  };
}

function whole(value: string): number | null {
  const parsed = Number(value.trim());
  return value.trim() !== '' && Number.isInteger(parsed) && parsed > 0 ? parsed : null;
}

/** The request body, or the fields that do not parse. The backend still checks every bound. */
export function toSettingsInput(draft: LoyaltySettingsDraft): { input?: LoyaltySettingsInput; bad: LoyaltySettingsField[] } {
  const satangPerPoint = parseBahtToSatang(draft.bahtPerPoint);
  const pointValue = parseBahtToSatang(draft.pointValueBaht);
  const silver = whole(draft.silver);
  const gold = whole(draft.gold);
  const months = draft.months.trim() === '' ? null : whole(draft.months);

  const bad: LoyaltySettingsField[] = [];
  if (!satangPerPoint) bad.push('bahtPerPoint');
  if (!pointValue) bad.push('pointValueBaht');
  if (!silver) bad.push('silver');
  if (!gold || (silver && gold <= silver)) bad.push('gold');
  if (draft.months.trim() !== '' && months === null) bad.push('months');

  if (bad.length > 0 || !satangPerPoint || !pointValue || !silver || !gold) {
    return { bad };
  }

  return {
    bad,
    input: {
      satang_per_point: satangPerPoint,
      point_value_satang: pointValue,
      silver_min_points: silver,
      gold_min_points: gold,
      points_expire_after_months: months,
    },
  };
}
