import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { ExplorePage } from './explore';

describe('explore page', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('filters societies by city and articles by category', () => {
    const page = TestBed.createComponent(ExplorePage).componentInstance;
    page.city.set('Lahore');
    expect(page.filteredSocieties().length).toBe(2);
    expect(page.filteredSocieties().every(s => s.city === 'Lahore')).toBe(true);

    page.category.set('Guide');
    expect(page.filteredArticles().length).toBe(2);
  });

  it('looks up a sample land record case-insensitively', () => {
    const page = TestBed.createComponent(ExplorePage).componentInstance;
    expect(page.record()).toBeNull();

    page.recordPlot.set('a-123');
    expect(page.record()?.city).toBe('Lahore');

    page.recordCity.set('Karachi');
    expect(page.record()).toBeNull();
  });
});
