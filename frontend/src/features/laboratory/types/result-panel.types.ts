/** The species a result panel carries reference ranges for. Any other species gets names and units only. */
export type PanelSpecies = 'dog' | 'cat';

/** A range as two numbers, or in words for a value that is not a number ("negative"). */
export type PanelRange = { low: number; high: number } | { text: string };

/** One value of a panel: its name as the analyser prints it, its unit, and a range per species. */
export interface PanelAnalyte {
  analyte: string;
  unit: string;
  ranges: Partial<Record<PanelSpecies, PanelRange>>;
}

export type ResultPanelId = 'cbc' | 'chem10' | 'ua';

/** A set of values entered together, picked by name or matched from a request's tests. */
export interface ResultPanel {
  id: ResultPanelId;
  label: string;
  /** Test codes that mean this panel, upper case. */
  codes: string[];
  /** Test names that mean this panel, lower case. */
  names: string[];
  analytes: PanelAnalyte[];
}
