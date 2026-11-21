import { TestBed } from '@angular/core/testing';
import { afterEach, describe, expect, it } from 'vitest';
import { PropertyDto } from './models';
import { SeoService } from './seo.service';

const property: PropertyDto = {
  id: 7,
  title: 'Test House',
  description: 'A test listing',
  price: 12500000,
  type: 'House',
  listingType: 'Sale',
  status: 'Active',
  city: 'Lahore',
  locality: 'DHA',
  address: 'DHA Phase 6, Lahore',
  latitude: 31.5,
  longitude: 74.3,
  bedrooms: 4,
  bathrooms: 3,
  areaMarla: 10,
  areaSqFt: 2250,
  imageUrls: ['https://example.com/house.jpg'],
  isFeatured: false,
  createdAt: '2026-01-01T00:00:00Z',
  verifiedDocuments: 2,
  totalDocuments: 3,
  isTrusted: true,
  agentId: null
};

describe('SeoService', () => {
  afterEach(() => {
    document.getElementById('property-json-ld')?.remove();
  });

  it('sets property metadata, canonical URL and JSON-LD', () => {
    const seo = TestBed.inject(SeoService);

    seo.setProperty(property);

    expect(document.title).toBe('Test House | RealEstateAI');
    expect(document.querySelector('link[rel="canonical"]')?.getAttribute('href'))
      .toBe('https://polite-paletas-4014d8.netlify.app/property/7');
    expect(document.querySelector('meta[property="og:image"]')?.getAttribute('content'))
      .toBe('https://example.com/house.jpg');

    const script = document.getElementById('property-json-ld');
    expect(script).not.toBeNull();
    const json = JSON.parse(script?.textContent ?? '{}');
    expect(json['@type']).toBe('Product');
    expect(json.offers.price).toBe(12500000);
    expect(json.offers.priceCurrency).toBe('PKR');
  });
});
