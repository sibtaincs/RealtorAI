import { Injectable, signal } from '@angular/core';

const FAVORITES_KEY = 'reai_favorites';
const RECENT_KEY = 'reai_recent';
const RECENT_LIMIT = 12;
const COMPARE_KEY = 'reai_compare';
const COMPARE_LIMIT = 3;

function readIds(key: string): number[] {
  try {
    const parsed = JSON.parse(localStorage.getItem(key) || '[]');
    return Array.isArray(parsed) ? parsed.filter((id): id is number => typeof id === 'number') : [];
  } catch {
    return [];
  }
}

@Injectable({ providedIn: 'root' })
export class ShortlistService {
  favorites = signal<number[]>(readIds(FAVORITES_KEY));
  recent = signal<number[]>(readIds(RECENT_KEY));
  compare = signal<number[]>(readIds(COMPARE_KEY));

  isFavorite(id: number): boolean {
    return this.favorites().includes(id);
  }

  toggle(id: number): void {
    const next = this.isFavorite(id)
      ? this.favorites().filter(value => value !== id)
      : [...this.favorites(), id];
    this.favorites.set(next);
    localStorage.setItem(FAVORITES_KEY, JSON.stringify(next));
  }

  viewed(id: number): void {
    const next = [id, ...this.recent().filter(value => value !== id)].slice(0, RECENT_LIMIT);
    this.recent.set(next);
    localStorage.setItem(RECENT_KEY, JSON.stringify(next));
  }

  isComparing(id: number): boolean {
    return this.compare().includes(id);
  }

  toggleCompare(id: number): void {
    const next = this.isComparing(id)
      ? this.compare().filter(value => value !== id)
      : [...this.compare(), id].slice(0, COMPARE_LIMIT);
    this.compare.set(next);
    localStorage.setItem(COMPARE_KEY, JSON.stringify(next));
  }

  clearCompare(): void {
    this.compare.set([]);
    localStorage.removeItem(COMPARE_KEY);
  }
}
