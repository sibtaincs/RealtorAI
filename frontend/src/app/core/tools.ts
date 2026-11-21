export const TOOL_AREA_UNITS = ['Marla', 'Kanal', 'Sq Ft', 'Sq Yd', 'Sq M'] as const;
export type ToolAreaUnit = typeof TOOL_AREA_UNITS[number];

const SQFT_PER_UNIT: Record<ToolAreaUnit, number> = {
  'Sq Ft': 1, Marla: 225, Kanal: 4500, 'Sq Yd': 9, 'Sq M': 10.7639
};

export function convertArea(value: number, from: ToolAreaUnit, to: ToolAreaUnit): number {
  return value * SQFT_PER_UNIT[from] / SQFT_PER_UNIT[to];
}

export function monthlyInstallment(principal: number, annualRatePct: number, years: number): number {
  if (principal <= 0 || years <= 0) return 0;
  const months = years * 12;
  const rate = annualRatePct / 1200;
  if (rate === 0) return principal / months;
  const factor = Math.pow(1 + rate, months);
  return principal * rate * factor / (factor - 1);
}

export function constructionCost(areaSqFt: number, ratePerSqFt: number): number {
  return Math.max(0, areaSqFt) * Math.max(0, ratePerSqFt);
}
