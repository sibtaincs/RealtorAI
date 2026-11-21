import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { Agency, NewProject } from '../core/directory.data';
import { AgenciesPage } from './agencies';
import { ProjectsPage } from './projects';

const projects: NewProject[] = [
  { name: 'P1', developer: 'D', city: 'Lahore', locality: 'A', type: 'Plots', priceFrom: 1, priceTo: 2, status: 'Launching' },
  { name: 'P2', developer: 'D', city: 'Lahore', locality: 'B', type: 'Houses', priceFrom: 1, priceTo: 2, status: 'Under Construction' },
  { name: 'P3', developer: 'D', city: 'Karachi', locality: 'C', type: 'Plots', priceFrom: 1, priceTo: 2, status: 'Launching' },
  { name: 'P4', developer: 'D', city: 'Islamabad', locality: 'D', type: 'Plots', priceFrom: 1, priceTo: 2, status: 'Under Construction' },
  { name: 'P5', developer: 'D', city: 'Islamabad', locality: 'E', type: 'Plots', priceFrom: 1, priceTo: 2, status: 'Nearing Completion' },
  { name: 'P6', developer: 'D', city: 'Multan', locality: 'F', type: 'Plots', priceFrom: 1, priceTo: 2, status: 'Launching' }
];

const agencies: Agency[] = [
  { name: 'A1', city: 'Lahore', agents: 1, listings: 1, rating: 4, verified: true, specialty: 'Residential' },
  { name: 'A2', city: 'Karachi', agents: 1, listings: 1, rating: 4, verified: true, specialty: 'Apartments & commercial' },
  { name: 'Capital Homes', city: 'Islamabad', agents: 1, listings: 1, rating: 4, verified: true, specialty: 'Houses & plots' },
  { name: 'A4', city: 'Rawalpindi', agents: 1, listings: 1, rating: 4, verified: false, specialty: 'Residential' },
  { name: 'A5', city: 'Karachi', agents: 1, listings: 1, rating: 4, verified: true, specialty: 'Commercial & rentals' },
  { name: 'A6', city: 'Multan', agents: 1, listings: 1, rating: 4, verified: false, specialty: 'Plots & farms' }
];

describe('directory filters', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
  });

  it('filters projects by city and status', () => {
    const page = TestBed.createComponent(ProjectsPage).componentInstance;
    page.projects.set(projects);
    expect(page.filtered().length).toBe(6);

    page.city.set('Lahore');
    expect(page.filtered().length).toBe(2);
    expect(page.filtered().every(p => p.city === 'Lahore')).toBe(true);

    page.status.set('Launching');
    expect(page.filtered().length).toBe(1);
  });

  it('filters agencies by city', () => {
    const page = TestBed.createComponent(AgenciesPage).componentInstance;
    page.agencies.set(agencies);
    page.city.set('Karachi');
    expect(page.filtered().length).toBe(2);
    expect(page.filtered().every(a => a.city === 'Karachi')).toBe(true);
  });

  it('filters agencies by property type', () => {
    const page = TestBed.createComponent(AgenciesPage).componentInstance;
    page.agencies.set(agencies);
    page.propertyType.set('Commercial');
    expect(page.filtered().length).toBe(2);
    expect(page.filtered().every(a => a.specialty.toLowerCase().includes('commercial'))).toBe(true);

    page.propertyType.set('Plots');
    page.city.set('Islamabad');
    expect(page.filtered().length).toBe(1);
    expect(page.filtered()[0].name).toBe('Capital Homes');
  });
});
