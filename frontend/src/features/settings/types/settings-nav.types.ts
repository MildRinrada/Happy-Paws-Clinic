import type { SettingsTab } from '../content/settings-tabs';

/** A page this session can use, and whether it still needs setting up. */
export interface VisibleSettingsTab extends SettingsTab {
  attention: boolean;
}

/** A group with at least one page left in it. */
export interface VisibleSettingsGroup {
  key: string;
  label: string;
  tabs: VisibleSettingsTab[];
}
