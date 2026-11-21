import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { Home } from './home';

const emptyResult = { items: [], total: 0, page: 1, pageSize: 9 };

describe('home dynamic search', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Home],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends advanced filters and sort to the API', () => {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/api/properties')).flush(emptyResult);

    const page = fixture.componentInstance;
    page.locality = 'DHA';
    page.minPrice = 1_000_000;
    page.maxPrice = 50_000_000;
    page.minMarla = 5;
    page.sort = 'price-asc';
    page.apply();

    const request = http.expectOne(r => r.url.includes('/api/properties'));
    expect(request.request.params.get('locality')).toBe('DHA');
    expect(request.request.params.get('minPrice')).toBe('1000000');
    expect(request.request.params.get('maxPrice')).toBe('50000000');
    expect(request.request.params.get('minMarla')).toBe('5');
    expect(request.request.params.get('sort')).toBe('price-asc');
    request.flush(emptyResult);

    expect(page.activeFilters().map(chip => chip.key)).toContain('sort');
  });

  it('blocks invalid price ranges without calling the API', () => {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/api/properties')).flush(emptyResult);

    const page = fixture.componentInstance;
    page.minPrice = 50_000_000;
    page.maxPrice = 1_000_000;
    page.apply();

    expect(page.filterError()).toContain('Min price');
    http.expectNone(r => r.url.includes('/api/properties'));
  });

  it('removes a single filter chip and refreshes results', () => {
    const fixture = TestBed.createComponent(Home);
    fixture.detectChanges();
    http.expectOne(r => r.url.includes('/api/properties')).flush(emptyResult);

    const page = fixture.componentInstance;
    page.city = 'Lahore';
    page.apply();
    http.expectOne(r => r.url.includes('/api/properties')).flush(emptyResult);

    page.removeFilter('city');
    const refreshed = http.expectOne(r => r.url.includes('/api/properties'));
    expect(refreshed.request.params.has('city')).toBe(false);
    refreshed.flush(emptyResult);
    expect(page.activeFilters().length).toBe(0);
  });
});
