import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { CommunityArticle, LandRecord, Society } from '../core/explore.data';
import { ExplorePage } from './explore';

const societies: Society[] = [
  { name: 'S1', city: 'Lahore', plotSizes: '5 marla', priceFrom: 1, status: 'Established' },
  { name: 'S2', city: 'Lahore', plotSizes: '10 marla', priceFrom: 1, status: 'Developing' },
  { name: 'S3', city: 'Karachi', plotSizes: '200 sq yd', priceFrom: 1, status: 'Established' }
];

const articles: CommunityArticle[] = [
  { title: 'G1', category: 'Guide', summary: '', date: '2026-01-01' },
  { title: 'G2', category: 'Guide', summary: '', date: '2026-01-02' },
  { title: 'N1', category: 'News', summary: '', date: '2026-01-03' }
];

const records: LandRecord[] = [
  { plot: 'A-123', city: 'Lahore', owner: 'Owner on file', area: '10 marla', status: 'Verified' },
  { plot: 'D-321', city: 'Islamabad', owner: 'Owner on file', area: '5 marla', status: 'Verified' }
];

describe('explore page', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('filters societies by city and articles by category', () => {
    const page = TestBed.createComponent(ExplorePage).componentInstance;
    page.societies.set(societies);
    page.articles.set(articles);
    page.city.set('Lahore');
    expect(page.filteredSocieties().length).toBe(2);
    expect(page.filteredSocieties().every(s => s.city === 'Lahore')).toBe(true);

    page.category.set('Guide');
    expect(page.filteredArticles().length).toBe(2);
  });

  it('looks up a land record case-insensitively', () => {
    const page = TestBed.createComponent(ExplorePage).componentInstance;
    page.landRecords.set(records);
    expect(page.record()).toBeNull();

    page.recordPlot.set('a-123');
    expect(page.record()?.city).toBe('Lahore');

    page.recordCity.set('Karachi');
    expect(page.record()).toBeNull();
  });
});
