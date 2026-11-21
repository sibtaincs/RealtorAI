export interface PropertyDto {
  id: number;
  title: string;
  description: string;
  price: number;
  type: string;
  listingType: string;
  status: string;
  city: string;
  locality: string;
  address: string;
  latitude: number;
  longitude: number;
  bedrooms: number | null;
  bathrooms: number | null;
  areaMarla: number | null;
  areaSqFt: number | null;
  imageUrls: string[];
  isFeatured: boolean;
  createdAt: string;
  verifiedDocuments: number;
  totalDocuments: number;
  isTrusted: boolean;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface PropertyQuery {
  search?: string;
  city?: string;
  locality?: string;
  type?: string;
  listingType?: string;
  minPrice?: number;
  maxPrice?: number;
  minBeds?: number;
  minMarla?: number;
  page?: number;
  pageSize?: number;
}

export interface PropertyInput {
  title: string;
  description: string;
  price: number;
  type: string;
  listingType: string;
  city: string;
  locality: string;
  address: string;
  latitude: number;
  longitude: number;
  bedrooms: number | null;
  bathrooms: number | null;
  areaMarla: number | null;
  areaSqFt: number | null;
  imageUrls: string[];
  isFeatured: boolean;
}

export interface LeadDto {
  id: number;
  name: string;
  email: string;
  phone: string;
  message: string;
  propertyListingId: number | null;
  propertyTitle: string | null;
  status: string;
  createdAt: string;
  score: number;
  scoreLabel: string;
  scoreReasons: string[];
}

export interface CreateLead {
  name: string;
  email: string;
  phone: string;
  message: string;
  propertyListingId: number | null;
}

export interface AuthResponse {
  token: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface AiChatResponse {
  reply: string;
  mode: string;
  recommendedPropertyIds: number[];
}

export interface PriceEstimate {
  estimatedPrice: number;
  low: number;
  high: number;
  basis: string;
}

export interface UploadResult {
  url: string;
  fileName: string;
  error?: string;
}

export interface DocumentItem {
  id: number;
  propertyListingId: number;
  name: string;
  notes: string | null;
  isVerified: boolean;
  verifiedAt: string | null;
  sortOrder: number;
}

export interface SavedSearch {
  id: number;
  name: string;
  queryJson: string;
  alertEnabled: boolean;
  createdAt: string;
  lastAlertAt: string | null;
}

export interface NotificationItem {
  id: number;
  title: string;
  message: string;
  link: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface FollowUp {
  id: number;
  leadId: number;
  channel: string;
  message: string;
  externalUrl: string | null;
  sent: boolean;
  createdAt: string;
}

// ---------- Phase 1 AI features ----------

export interface GeneratedListing {
  title: string;
  description: string;
  type: string;
  listingType: string;
  city: string;
  locality: string;
  bedrooms: number | null;
  bathrooms: number | null;
  areaMarla: number | null;
  price: number;
  highlights: string[];
  mode: string;
}

export interface SellPriceResponse {
  recommendedPrice: number;
  low: number;
  high: number;
  targetDays: number;
  confidence: string;
  basis: string;
  advice: string;
}

export interface QualityCheck {
  name: string;
  points: number;
  maxPoints: number;
  done: boolean;
  tip: string;
}

export interface ListingQualityResponse {
  score: number;
  label: string;
  checks: QualityCheck[];
}

export interface WhatsAppReply {
  reply: string;
  recommendedPropertyIds: number[];
}

// ---------- Phase 2: OCR, trust badges, fraud detection ----------

export interface OcrScanResult {
  matched: number;
  newlyVerified: number;
  matchedNames: string[];
  items: DocumentItem[];
}

export interface DuplicateMatch {
  propertyId: number;
  title: string;
  similarity: number;
  reasons: string[];
}

export interface DuplicateCheckInput {
  title: string;
  city: string;
  locality: string;
  type: string;
  listingType: string;
  areaMarla: number | null;
  price: number;
  imageUrls: string[];
}

export interface DuplicateCheckResponse {
  risk: string;
  score: number;
  matches: DuplicateMatch[];
  warnings: string[];
  advice: string;
}

export interface RiskReport {
  score: number;
  label: string;
  marketMedian: string;
  flags: string[];
  verdict: string;
}

// ---------- Phase 3: agent dashboard ----------

export interface DashboardSummary {
  newLeads7Days: number;
  totalLeads: number;
  hotLeads: number;
  warmLeads: number;
  coldLeads: number;
  activeListings: number;
  verifiedListings: number;
  averageFirstResponseHours: number;
  unreadNotifications: number;
}

export interface ListingPerformance {
  id: number;
  title: string;
  city: string;
  status: string;
  leadCount: number;
  verifiedDocuments: number;
  totalDocuments: number;
  isTrusted: boolean;
  daysListed: number;
}

export interface NotificationPreferences {
  inAppEnabled: boolean;
  emailEnabled: boolean;
  whatsappEnabled: boolean;
}

export const PROPERTY_TYPES = ['House', 'Plot', 'Apartment', 'Shop', 'Office', 'Villa', 'Farmhouse'];
export const CITIES = ['Karachi', 'Lahore', 'Islamabad', 'Rawalpindi', 'Multan', 'Faisalabad', 'Gujranwala', 'Peshawar', 'Hyderabad', 'Quetta'];

export function formatPkr(amount: number, listingType?: string): string {
  const suffix = listingType === 'Rent' ? '/month' : '';
  if (amount >= 1e7) return `${(amount / 1e7).toFixed(2).replace(/\.00$/, '')} crore PKR${suffix}`;
  if (amount >= 1e5) return `${(amount / 1e5).toFixed(2).replace(/\.00$/, '')} lakh PKR${suffix}`;
  return `${amount.toLocaleString('en-PK')} PKR${suffix}`;
}
