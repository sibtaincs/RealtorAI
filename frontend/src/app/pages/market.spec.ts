import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { MarketPage } from './market';

describe('market page filters', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('filters guides, trends and index by city', () => {
    const page = TestBed.createComponent(MarketPage).componentInstance;
    expect(page.filteredGuides().length).toBe(6);

    page.city.set('Lahore');
    expect(page.filteredGuides().length).toBe(2);
    expect(page.filteredTrends().length).toBe(2);
    expect(page.filteredIndex().length).toBe(1);
    expect(page.filteredIndex()[0].city).toBe('Lahore');
  });

  it('handles an unknown city with empty sections', () => {
    const page = TestBed.createComponent(MarketPage).componentInstance;
    page.city.set('Quetta');
    expect(page.filteredGuides()).toEqual([]);
    expect(page.filteredTrends()).toEqual([]);
    expect(page.filteredIndex()).toEqual([]);
  });
});
