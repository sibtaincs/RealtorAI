export interface NewProject {
  name: string;
  developer: string;
  city: string;
  locality: string;
  type: string;
  priceFrom: number;
  priceTo: number;
  status: 'Launching' | 'Under Construction' | 'Nearing Completion';
}

export interface Agency {
  name: string;
  city: string;
  agents: number;
  listings: number;
  rating: number;
  verified: boolean;
  specialty: string;
}

// Sample directory data for browsing; replace with API data when a feed is available.
export const NEW_PROJECTS: NewProject[] = [
  { name: 'DHA Phase 9 Prism', developer: 'DHA Lahore', city: 'Lahore', locality: 'Phase 9', type: 'Plots & Villas', priceFrom: 4500000, priceTo: 18000000, status: 'Launching' },
  { name: 'Bahria Town Orchard', developer: 'Bahria Town', city: 'Lahore', locality: 'Bahria Orchard', type: 'Houses', priceFrom: 8500000, priceTo: 22000000, status: 'Under Construction' },
  { name: 'Bahria Bay View', developer: 'Bahria Town', city: 'Karachi', locality: 'Clifton', type: 'Apartments', priceFrom: 6200000, priceTo: 19500000, status: 'Nearing Completion' },
  { name: 'Capital Enclave', developer: 'Capital Development Authority', city: 'Islamabad', locality: 'E-11', type: 'Plots', priceFrom: 7000000, priceTo: 24000000, status: 'Launching' },
  { name: 'Gulberg Greens Extension', developer: 'Gulberg Group', city: 'Islamabad', locality: 'Gulberg Greens', type: 'Plots & Farms', priceFrom: 3800000, priceTo: 14000000, status: 'Under Construction' },
  { name: 'DHA City Multan', developer: 'DHA Multan', city: 'Multan', locality: 'Bosan Road', type: 'Plots', priceFrom: 1800000, priceTo: 6200000, status: 'Nearing Completion' }
];

export const AGENCIES: Agency[] = [
  { name: 'RealEstate AI Realty', city: 'Lahore', agents: 12, listings: 84, rating: 4.8, verified: true, specialty: 'Residential & plots' },
  { name: 'Crescent Property Group', city: 'Karachi', agents: 9, listings: 61, rating: 4.6, verified: true, specialty: 'Apartments & commercial' },
  { name: 'Capital Homes', city: 'Islamabad', agents: 7, listings: 45, rating: 4.5, verified: true, specialty: 'Houses & plots' },
  { name: 'Green Valley Estates', city: 'Rawalpindi', agents: 5, listings: 33, rating: 4.2, verified: false, specialty: 'Residential' },
  { name: 'Sindh Realty Hub', city: 'Karachi', agents: 8, listings: 52, rating: 4.4, verified: true, specialty: 'Commercial & rentals' },
  { name: 'Mehr Properties', city: 'Multan', agents: 4, listings: 27, rating: 4.1, verified: false, specialty: 'Plots & farms' }
];
