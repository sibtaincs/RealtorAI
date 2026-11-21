export interface Society {
  name: string;
  city: string;
  plotSizes: string;
  priceFrom: number;
  status: string;
}

export interface LandRecord {
  plot: string;
  city: string;
  owner: string;
  area: string;
  status: string;
}

export interface CommunityArticle {
  title: string;
  category: 'News' | 'Guide' | 'Community';
  summary: string;
  date: string;
}

// Sample data for browsing; society maps and land records need a real data feed for factual use.
export const SOCIETIES: Society[] = [
  { name: 'DHA Lahore', city: 'Lahore', plotSizes: '5 / 10 marla, 1 kanal', priceFrom: 4500000, status: 'Established' },
  { name: 'Bahria Town Lahore', city: 'Lahore', plotSizes: '5 / 10 marla, 1 kanal', priceFrom: 3200000, status: 'Established' },
  { name: 'DHA Karachi', city: 'Karachi', plotSizes: '120 / 200 sq yd', priceFrom: 9000000, status: 'Established' },
  { name: 'Bahria Town Karachi', city: 'Karachi', plotSizes: '125 / 250 sq yd', priceFrom: 3500000, status: 'Developing' },
  { name: 'DHA Islamabad', city: 'Islamabad', plotSizes: '10 marla, 1 kanal', priceFrom: 7000000, status: 'Established' },
  { name: 'Gulberg Greens', city: 'Islamabad', plotSizes: '5 marla – 4 kanal', priceFrom: 3800000, status: 'Developing' }
];

export const LAND_RECORDS: LandRecord[] = [
  { plot: 'A-123', city: 'Lahore', owner: 'Sample Owner', area: '10 marla', status: 'Verified' },
  { plot: 'B-456', city: 'Lahore', owner: 'Sample Owner', area: '1 kanal', status: 'Pending verification' },
  { plot: 'C-789', city: 'Karachi', owner: 'Sample Owner', area: '200 sq yd', status: 'Verified' },
  { plot: 'D-321', city: 'Islamabad', owner: 'Sample Owner', area: '5 marla', status: 'Verified' }
];

export const COMMUNITY_ARTICLES: CommunityArticle[] = [
  { title: 'How to verify a property before paying a deposit', category: 'Guide', summary: 'Documents, ownership checks and the questions to ask your agent.', date: '2026-11-10' },
  { title: 'Rental demand rises in Lahore suburbs', category: 'News', summary: 'Sample market story showing which areas buyers are searching most.', date: '2026-11-08' },
  { title: 'Understanding marla, kanal and square feet', category: 'Guide', summary: 'A quick unit primer for first-time buyers.', date: '2026-11-04' },
  { title: 'Community: share your home-buying checklist', category: 'Community', summary: 'Discussion starter for new buyers and agents.', date: '2026-11-01' }
];
