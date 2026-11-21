import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { AgenciesPage } from './agencies';
import { ProjectsPage } from './projects';

describe('directory filters', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({});
  });

  it('filters projects by city and status', () => {
    const page = TestBed.createComponent(ProjectsPage).componentInstance;
    expect(page.filtered().length).toBe(6);

    page.city.set('Lahore');
    expect(page.filtered().length).toBe(2);
    expect(page.filtered().every(p => p.city === 'Lahore')).toBe(true);

    page.status.set('Launching');
    expect(page.filtered().length).toBe(1);
  });

  it('filters agencies by city', () => {
    const page = TestBed.createComponent(AgenciesPage).componentInstance;
    page.city.set('Karachi');
    expect(page.filtered().length).toBe(2);
    expect(page.filtered().every(a => a.city === 'Karachi')).toBe(true);
  });

  it('filters agencies by property type', () => {
    const page = TestBed.createComponent(AgenciesPage).componentInstance;
    page.propertyType.set('Commercial');
    expect(page.filtered().length).toBe(2);
    expect(page.filtered().every(a => a.specialty.toLowerCase().includes('commercial'))).toBe(true);

    page.propertyType.set('Plots');
    page.city.set('Islamabad');
    expect(page.filtered().length).toBe(1);
    expect(page.filtered()[0].name).toBe('Capital Homes');
  });
});
