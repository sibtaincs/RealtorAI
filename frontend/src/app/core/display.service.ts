import { Injectable, signal } from '@angular/core';
import { formatPkr } from './models';

export const CURRENCIES = ['PKR', 'USD', 'AED', 'SAR', 'GBP', 'CAD'] as const;
export const AREA_UNITS = ['Marla', 'Kanal', 'Sq Ft', 'Sq Yd', 'Sq M'] as const;

export type Currency = typeof CURRENCIES[number];
export type AreaUnit = typeof AREA_UNITS[number];

const PKR_PER_UNIT: Record<Currency, number> = {
  PKR: 1, USD: 278, AED: 75.7, SAR: 74.1, GBP: 352, CAD: 203
};

const SQFT_PER_UNIT: Record<AreaUnit, number> = {
  'Sq Ft': 1, Marla: 225, Kanal: 4500, 'Sq Yd': 9, 'Sq M': 10.7639
};

@Injectable({ providedIn: 'root' })
export class DisplayService {
  currency = signal<Currency>(this.read('reai_currency', CURRENCIES, 'PKR'));
  areaUnit = signal<AreaUnit>(this.read('reai_area_unit', AREA_UNITS, 'Marla'));

  money(amountPkr: number, listingType?: string): string {
    if (this.currency() === 'PKR') return formatPkr(amountPkr, listingType);
    const value = amountPkr / PKR_PER_UNIT[this.currency()];
    const formatted = new Intl.NumberFormat('en-US', {
      maximumFractionDigits: value >= 1000 ? 0 : 1
    }).format(value);
    return `${formatted} ${this.currency()}${listingType === 'Rent' ? '/month' : ''}`;
  }

  area(areaMarla: number | null, areaSqFt: number | null): string {
    const sqft = areaSqFt || (areaMarla ? areaMarla * 225 : 0);
    if (!sqft) return '';
    const value = sqft / SQFT_PER_UNIT[this.areaUnit()];
    const formatted = value.toLocaleString('en-US', {
      maximumFractionDigits: value < 10 ? 2 : 1
    });
    return `${formatted} ${this.areaUnit()}`;
  }

  setCurrency(value: Currency): void {
    this.currency.set(value);
    this.persist('reai_currency', value);
  }

  setAreaUnit(value: AreaUnit): void {
    this.areaUnit.set(value);
    this.persist('reai_area_unit', value);
  }

  private read<T extends string>(key: string, allowed: readonly T[], fallback: T): T {
    const stored = typeof localStorage !== 'undefined' ? localStorage.getItem(key) : null;
    return allowed.includes(stored as T) ? stored as T : fallback;
  }

  private persist(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // Display preferences are cosmetic; ignore blocked storage.
    }
  }
}
