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
