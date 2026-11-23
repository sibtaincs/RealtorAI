import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { DisplayService } from './display.service';
import { constructionCost, convertArea, estimateValue, monthlyInstallment, rentalYieldPct, registrationCostPkr, stampDutyPkr } from './tools';

describe('tool calculations', () => {
  it('amortises a home loan', () => {
    expect(monthlyInstallment(120000, 0, 10)).toBe(1000);
    const monthly = monthlyInstallment(1000000, 12, 20);
    expect(monthly).toBeGreaterThan(10000);
    expect(monthly).toBeLessThan(12000);
  });

  it('returns zero for invalid loan terms', () => {
    expect(monthlyInstallment(0, 12, 20)).toBe(0);
    expect(monthlyInstallment(1000000, 12, 0)).toBe(0);
  });

  it('converts between area units both ways', () => {
    expect(convertArea(1, 'Marla', 'Sq Ft')).toBe(225);
    expect(convertArea(1, 'Kanal', 'Marla')).toBe(20);
    expect(convertArea(225, 'Sq Ft', 'Marla')).toBe(1);
  });

  it('computes construction cost without negatives', () => {
    expect(constructionCost(1000, 3000)).toBe(3_000_000);
    expect(constructionCost(-5, 3000)).toBe(0);
  });

  it('estimates value without negatives', () => {
    expect(estimateValue(1000, 18000)).toBe(18_000_000);
    expect(estimateValue(-1, 18000)).toBe(0);
  });

  it('computes rental yield as annual rent over value', () => {
    expect(rentalYieldPct(25000, 5_000_000)).toBe(6);
    expect(rentalYieldPct(0, 5_000_000)).toBe(0);
    expect(rentalYieldPct(25000, 0)).toBe(0);
  });

  it('computes indicative stamp duty and registration cost', () => {
    expect(stampDutyPkr(10_000_000)).toBe(700_000);
    expect(registrationCostPkr(10_000_000)).toBe(200_000);
    expect(stampDutyPkr(-1)).toBe(0);
    expect(registrationCostPkr(-1)).toBe(0);
  });
});

describe('DisplayService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('formats money in the selected currency and persists it', () => {
    const display = TestBed.inject(DisplayService);
    expect(display.money(100000)).toContain('lakh PKR');
    expect(display.money(1234.56)).toBe('1,235 PKR');

    display.setCurrency('USD');
    expect(display.money(278000)).toBe('1,000 USD');
    expect(localStorage.getItem('reai_currency')).toBe('USD');
  });

  it('formats the selected area unit', () => {
    const display = TestBed.inject(DisplayService);
    expect(display.area(1, null)).toBe('1 Marla');

    display.setAreaUnit('Kanal');
    expect(display.area(1, null)).toBe('0.05 Kanal');
    expect(display.area(null, 900)).toBe('0.2 Kanal');
  });

  it('ignores unknown stored preferences', () => {
    localStorage.setItem('reai_currency', 'BTC');
    localStorage.setItem('reai_area_unit', 'Acre');
    const display = TestBed.inject(DisplayService);
    expect(display.currency()).toBe('PKR');
    expect(display.areaUnit()).toBe('Marla');
  });
});
