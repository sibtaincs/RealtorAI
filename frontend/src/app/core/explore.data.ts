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
