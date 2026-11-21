import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { AreaGuide, AreaTrend, PriceIndexRow } from '../core/market.data';
import { MarketPage } from './market';

const guides: AreaGuide[] = [
  { city: 'Lahore', name: 'G1', summary: '', pricePerSqFt: 1, safety: '', commute: '', schools: '' },
  { city: 'Lahore', name: 'G2', summary: '', pricePerSqFt: 1, safety: '', commute: '', schools: '' },
  { city: 'Karachi', name: 'G3', summary: '', pricePerSqFt: 1, safety: '', commute: '', schools: '' },
  { city: 'Karachi', name: 'G4', summary: '', pricePerSqFt: 1, safety: '', commute: '', schools: '' },
  { city: 'Islamabad', name: 'G5', summary: '', pricePerSqFt: 1, safety: '', commute: '', schools: '' },
  { city: 'Islamabad', name: 'G6', summary: '', pricePerSqFt: 1, safety: '', commute: '', schools: '' }
];

const trends: AreaTrend[] = [
  { city: 'Lahore', area: 'A', priceChangePct: 1, searchChangePct: 1 },
  { city: 'Lahore', area: 'B', priceChangePct: 1, searchChangePct: 1 },
  { city: 'Karachi', area: 'C', priceChangePct: 1, searchChangePct: 1 }
];

const index: PriceIndexRow[] = [
  { city: 'Lahore', index: 100, yearChangePct: 1, medianPrice: 1 },
  { city: 'Karachi', index: 100, yearChangePct: 1, medianPrice: 1 }
];

describe('market page filters', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('filters guides, trends and index by city', () => {
    const page = TestBed.createComponent(MarketPage).componentInstance;
    page.guides.set(guides);
    page.trends.set(trends);
    page.index.set(index);
    expect(page.filteredGuides().length).toBe(6);

    page.city.set('Lahore');
    expect(page.filteredGuides().length).toBe(2);
    expect(page.filteredTrends().length).toBe(2);
    expect(page.filteredIndex().length).toBe(1);
    expect(page.filteredIndex()[0].city).toBe('Lahore');
  });

  it('handles an unknown city with empty sections', () => {
    const page = TestBed.createComponent(MarketPage).componentInstance;
    page.guides.set(guides);
    page.trends.set(trends);
    page.index.set(index);
    page.city.set('Quetta');
    expect(page.filteredGuides()).toEqual([]);
    expect(page.filteredTrends()).toEqual([]);
    expect(page.filteredIndex()).toEqual([]);
  });
});
