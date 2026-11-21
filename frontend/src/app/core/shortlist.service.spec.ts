import { TestBed } from '@angular/core/testing';
import { ShortlistService } from './shortlist.service';

describe('ShortlistService', () => {
  let service: ShortlistService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ShortlistService);
  });

  it('toggles a favourite and persists it', () => {
    service.toggle(7);
    expect(service.isFavorite(7)).toBe(true);
    expect(JSON.parse(localStorage.getItem('reai_favorites') || '[]')).toEqual([7]);

    service.toggle(7);
    expect(service.isFavorite(7)).toBe(false);
  });

  it('keeps recent views unique and capped', () => {
    for (let id = 1; id <= 13; id++) service.viewed(id);
    service.viewed(1);

    expect(service.recent().length).toBe(12);
    expect(service.recent()[0]).toBe(1);
    expect(service.recent().filter(id => id === 1).length).toBe(1);
    expect(JSON.parse(localStorage.getItem('reai_recent') || '[]').length).toBe(12);
  });
});
