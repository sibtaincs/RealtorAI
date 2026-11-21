export interface AreaGuide {
  city: string;
  name: string;
  summary: string;
  pricePerSqFt: number;
  safety: string;
  commute: string;
  schools: string;
}

export interface AreaTrend {
  city: string;
  area: string;
  priceChangePct: number;
  searchChangePct: number;
}

export interface PriceIndexRow {
  city: string;
  index: number;
  yearChangePct: number;
  medianPrice: number;
}
