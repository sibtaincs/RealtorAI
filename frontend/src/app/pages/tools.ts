import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DisplayService } from '../core/display.service';
import { TOOL_AREA_UNITS, ToolAreaUnit, constructionCost, convertArea, estimateValue, monthlyInstallment, rentalYieldPct, registrationCostPkr, stampDutyPkr } from '../core/tools';
import { PriceEstimate, SellPriceResponse } from '../core/models';
import { MasterDataService } from '../core/master-data.service';
import { AiService } from '../core/ai.service';

@Component({
  selector: 'app-tools',
  imports: [FormsModule, DecimalPipe, RouterLink],
  templateUrl: './tools.html',
  styleUrl: './tools.scss'
})
export class ToolsPage {
  display = inject(DisplayService);
  master = inject(MasterDataService);
  private ai = inject(AiService);
  areaUnits = TOOL_AREA_UNITS;

  toolLinks = [
    { icon: '🗺️', label: 'Plot Finder', route: '/explore', fragment: 'plot-finder' },
    { icon: '🏷️', label: 'Z-Value', route: '/tools', fragment: 'valuation' },
    { icon: '🏦', label: 'Home Loan Calculator', route: '/tools', fragment: 'emi' },
    { icon: '🏗️', label: 'Construction Cost Calculator', route: '/tools', fragment: 'construction' },
    { icon: '💰', label: 'Rental Yield & Costs', route: '/tools', fragment: 'yield' },
    { icon: '📐', label: 'Area Unit Converter', route: '/tools', fragment: 'area' },
    { icon: '📍', label: 'Area Guide', route: '/market', fragment: 'area-guides' },
    { icon: '📄', label: 'Land Record', route: '/explore', fragment: 'land-records' },
    { icon: '📈', label: 'Property Search Trends', route: '/market', fragment: 'trends' },
    { icon: '📊', label: 'Property Price Index', route: '/market', fragment: 'price-index' }
  ];

  zCity = 'Lahore';
  zType = 'House';
  zMarla = 5;
  zTargetDays = 30;
  zResult: SellPriceResponse | null = null;
  zBusy = false;

  loanAmount = 5000000;
  loanRate = 12;
  loanYears = 20;

  yieldValue = 5000000;
  yieldRent = 250000;

  buildCity = 'Lahore';
  buildMarla = 5;
  buildFloors = 2;
  buildQuality: 'standard' | 'premium' = 'standard';
  buildMode: 'grey' | 'complete' = 'complete';

  buildPresets = [3, 4, 5, 6, 7, 8, 10, 20];

  priceCity = 'Lahore';
  priceLocality = '';
  priceType = 'House';
  priceMarla = 5;
  priceBeds = 3;
  priceEstimate: PriceEstimate | null = null;
  priceBusy = false;

  estimatePrice(): void {
    this.priceBusy = true;
    this.priceEstimate = null;
    this.ai.estimate({
      city: this.priceCity,
      locality: this.priceLocality || this.priceCity,
      type: this.priceType,
      areaMarla: this.priceMarla,
      bedrooms: this.priceBeds
    }).subscribe({
      next: res => { this.priceEstimate = res; this.priceBusy = false; },
      error: () => { this.priceBusy = false; }
    });
  }

  private cityFactor: Record<string, number> = {
    Karachi: 1.08, Lahore: 1, Islamabad: 1.12, Rawalpindi: 1.05,
    Multan: 0.95, Faisalabad: 0.97, Peshawar: 1.0, Hyderabad: 1.0
  };

  private baseRate(mode: 'grey' | 'complete', quality: 'standard' | 'premium'): number {
    const table: Record<string, Record<string, number>> = {
      grey: { standard: 1800, premium: 2200 },
      complete: { standard: 2800, premium: 3500 }
    };
    return table[mode][quality];
  }

  get buildAreaSqFt(): number { return this.buildMarla * 225 * this.buildFloors; }
  get buildRatePerSqFt(): number { return Math.round(this.baseRate(this.buildMode, this.buildQuality) * (this.cityFactor[this.buildCity] ?? 1)); }
  get buildCost(): number { return constructionCost(this.buildAreaSqFt, this.buildRatePerSqFt); }
  get buildGreyRate(): number { return Math.round(this.baseRate('grey', this.buildQuality) * (this.cityFactor[this.buildCity] ?? 1)); }
  get buildGreyCost(): number { return constructionCost(this.buildAreaSqFt, this.buildGreyRate); }
  get buildFinishingCost(): number { return Math.max(0, this.buildCost - this.buildGreyCost); }
  convertValue = 1;
  convertFrom: ToolAreaUnit = 'Marla';
  convertTo: ToolAreaUnit = 'Sq Ft';

  estimateZValue(): void {
    this.zBusy = true;
    this.zResult = null;
    this.ai.sellPrice({
      type: this.zType,
      city: this.zCity,
      locality: this.zCity,
      areaMarla: this.zMarla,
      targetDays: this.zTargetDays
    }).subscribe({
      next: res => { this.zResult = res; this.zBusy = false; },
      error: () => { this.zBusy = false; }
    });
  }

  get emi(): number { return monthlyInstallment(this.loanAmount, this.loanRate, this.loanYears); }
  get totalPayable(): number { return this.emi * this.loanYears * 12; }
  get totalInterest(): number { return this.totalPayable - this.loanAmount; }
  get rentalYield(): number { return rentalYieldPct(this.yieldRent, this.yieldValue); }
  get stampDuty(): number { return stampDutyPkr(this.yieldValue); }
  get registration(): number { return registrationCostPkr(this.yieldValue); }
  get converted(): number { return convertArea(this.convertValue, this.convertFrom, this.convertTo); }
  get areaUnitOptions(): { value: ToolAreaUnit; label: string }[] {
    const loaded = this.master.options('area-unit')
      .filter(o => (TOOL_AREA_UNITS as readonly string[]).includes(o.value));
    return loaded.length
      ? loaded.map(o => ({ value: o.value as ToolAreaUnit, label: o.label }))
      : TOOL_AREA_UNITS.map(u => ({ value: u, label: u }));
  }
}
