import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { ToolsPage } from './tools';

describe('tools hub', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('links all ten options to their sections', () => {
    const page = TestBed.createComponent(ToolsPage).componentInstance;
    expect(page.toolLinks.length).toBe(10);
    expect(page.toolLinks.map(l => l.fragment)).toContain('valuation');
    expect(page.toolLinks.map(l => l.fragment)).toContain('land-records');
  });

  it('estimates Z-Value from comparable listings', () => {
    const page = TestBed.createComponent(ToolsPage).componentInstance;
    page.zCity = 'Lahore';
    page.zType = 'House';
    page.zMarla = 5;
    page.zTargetDays = 30;

    page.estimateZValue();

    const req = httpMock.expectOne((r) => r.url.endsWith('/ai/sell-price'));
    expect(req.request.method).toBe('POST');
    req.flush({
      recommendedPrice: 9300000,
      low: 8850000,
      high: 9750000,
      targetDays: 30,
      confidence: 'Medium',
      basis: 'Median price-per-marla of 5 similar listings',
      advice: 'Price is market-aligned.'
    });

    expect(page.zResult?.recommendedPrice).toBe(9300000);
    expect(page.zResult?.confidence).toBe('Medium');
    expect(page.zBusy).toBe(false);
  });
});
