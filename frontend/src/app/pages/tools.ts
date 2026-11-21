import { DecimalPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ContentService } from '../core/content.service';
import { DisplayService } from '../core/display.service';
import { AreaGuide } from '../core/market.data';
import { TOOL_AREA_UNITS, ToolAreaUnit, constructionCost, convertArea, estimateValue, monthlyInstallment } from '../core/tools';
import { MasterDataService } from '../core/master-data.service';

@Component({
  selector: 'app-tools',
  imports: [FormsModule, DecimalPipe, RouterLink],
  templateUrl: './tools.html',
  styleUrl: './tools.scss'
})
export class ToolsPage implements OnInit {
  display = inject(DisplayService);
  private content = inject(ContentService);
  master = inject(MasterDataService);
  areaUnits = TOOL_AREA_UNITS;

  toolLinks = [
    { icon: '🗺️', label: 'Plot Finder', route: '/explore', fragment: 'plot-finder' },
    { icon: '🏷️', label: 'Z-Value', route: '/tools', fragment: 'valuation' },
    { icon: '🏦', label: 'Home Loan Calculator', route: '/tools', fragment: 'emi' },
    { icon: '🏗️', label: 'Construction Cost Calculator', route: '/tools', fragment: 'construction' },
    { icon: '📐', label: 'Area Unit Converter', route: '/tools', fragment: 'area' },
    { icon: '📍', label: 'Area Guide', route: '/market', fragment: 'area-guides' },
    { icon: '📄', label: 'Land Record', route: '/explore', fragment: 'land-records' },
    { icon: '📈', label: 'Property Search Trends', route: '/market', fragment: 'trends' },
    { icon: '📊', label: 'Property Price Index', route: '/market', fragment: 'price-index' }
  ];

  valuationAreas = signal<AreaGuide[]>([]);
  valuationArea = signal('');
  valuationSqFt = 2250;

  loanAmount = 5000000;
  loanRate = 12;
  loanYears = 20;

  buildCity = 'Lahore';
  buildMarla = 5;
  buildFloors = 2;
  buildQuality: 'standard' | 'premium' = 'standard';
  buildMode: 'grey' | 'complete' = 'complete';

  buildPresets = [3, 4, 5, 6, 7, 8, 10, 20];

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

  ngOnInit(): void {
    this.content.list<AreaGuide>('area-guide').subscribe(items => {
      this.valuationAreas.set(items);
      if (items.length && !items.some(a => a.name === this.valuationArea())) this.valuationArea.set(items[0].name);
    });
  }

  get valuationRate(): number { return this.valuationAreas().find(a => a.name === this.valuationArea())?.pricePerSqFt ?? 0; }
  get estimatedValue(): number { return estimateValue(this.valuationSqFt, this.valuationRate); }
  get emi(): number { return monthlyInstallment(this.loanAmount, this.loanRate, this.loanYears); }
  get totalPayable(): number { return this.emi * this.loanYears * 12; }
  get totalInterest(): number { return this.totalPayable - this.loanAmount; }
  get converted(): number { return convertArea(this.convertValue, this.convertFrom, this.convertTo); }
  get areaUnitOptions(): { value: ToolAreaUnit; label: string }[] {
    const loaded = this.master.options('area-unit')
      .filter(o => (TOOL_AREA_UNITS as readonly string[]).includes(o.value));
    return loaded.length
      ? loaded.map(o => ({ value: o.value as ToolAreaUnit, label: o.label }))
      : TOOL_AREA_UNITS.map(u => ({ value: u, label: u }));
  }
}
