import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { ToolsPage } from './tools';

describe('tools hub', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()]
    });
  });

  it('links all nine options to their sections', () => {
    const page = TestBed.createComponent(ToolsPage).componentInstance;
    expect(page.toolLinks.length).toBe(9);
    expect(page.toolLinks.map(l => l.fragment)).toContain('valuation');
    expect(page.toolLinks.map(l => l.fragment)).toContain('land-records');
  });

  it('estimates value from the selected sample area rate', () => {
    const page = TestBed.createComponent(ToolsPage).componentInstance;
    page.valuationArea = 'DHA Phase 6';
    page.valuationSqFt = 1000;
    expect(page.valuationRate).toBe(18000);
    expect(page.estimatedValue).toBe(18_000_000);

    page.valuationSqFt = -1;
    expect(page.estimatedValue).toBe(0);
  });
});
