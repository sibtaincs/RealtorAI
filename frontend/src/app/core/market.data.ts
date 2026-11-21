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

// Sample market data for browsing; replace with a real feed when one is available.
export const AREA_GUIDES: AreaGuide[] = [
  { city: 'Lahore', name: 'DHA Phase 6', summary: 'Established family locality with wide roads and mature amenities.', pricePerSqFt: 18000, safety: 'High', commute: '30 min to centre', schools: 'Strong' },
  { city: 'Lahore', name: 'Johar Town', summary: 'Budget-friendly apartments and houses with good transit links.', pricePerSqFt: 9500, safety: 'Medium', commute: '25 min to centre', schools: 'Good' },
  { city: 'Karachi', name: 'Clifton', summary: 'Premium seafront apartments and commercial addresses.', pricePerSqFt: 26000, safety: 'High', commute: '20 min to centre', schools: 'Strong' },
  { city: 'Karachi', name: 'Gulshan-e-Iqbal', summary: 'Dense residential area with strong rental demand.', pricePerSqFt: 12000, safety: 'Medium', commute: '35 min to centre', schools: 'Good' },
  { city: 'Islamabad', name: 'DHA Phase 2', summary: 'Plots and villas near the airport and commercial hubs.', pricePerSqFt: 21000, safety: 'High', commute: '20 min to centre', schools: 'Strong' },
  { city: 'Islamabad', name: 'G-11', summary: 'Quiet sectors with parks and easy access to the motorway.', pricePerSqFt: 14000, safety: 'High', commute: '25 min to centre', schools: 'Good' }
];

export const AREA_TRENDS: AreaTrend[] = [
  { city: 'Lahore', area: 'DHA Phase 6', priceChangePct: 8.4, searchChangePct: 17 },
  { city: 'Lahore', area: 'Bahria Town', priceChangePct: 5.1, searchChangePct: 9 },
  { city: 'Karachi', area: 'Clifton', priceChangePct: 6.8, searchChangePct: 12 },
  { city: 'Karachi', area: 'Gulshan-e-Iqbal', priceChangePct: 3.2, searchChangePct: 6 },
  { city: 'Islamabad', area: 'DHA Phase 2', priceChangePct: 7.6, searchChangePct: 14 },
  { city: 'Islamabad', area: 'G-11', priceChangePct: 4.5, searchChangePct: 8 }
];

export const PRICE_INDEX: PriceIndexRow[] = [
  { city: 'Lahore', index: 184, yearChangePct: 7.2, medianPrice: 14500000 },
  { city: 'Karachi', index: 201, yearChangePct: 6.1, medianPrice: 17200000 },
  { city: 'Islamabad', index: 176, yearChangePct: 8.3, medianPrice: 19800000 },
  { city: 'Rawalpindi', index: 152, yearChangePct: 5.4, medianPrice: 9200000 },
  { city: 'Multan', index: 138, yearChangePct: 4.7, medianPrice: 5800000 }
];
