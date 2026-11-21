import { DecimalPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DisplayService } from '../core/display.service';
import { TOOL_AREA_UNITS, ToolAreaUnit, constructionCost, convertArea, monthlyInstallment } from '../core/tools';

@Component({
  selector: 'app-tools',
  imports: [FormsModule, DecimalPipe],
  templateUrl: './tools.html',
  styleUrl: './tools.scss'
})
export class ToolsPage {
  display = inject(DisplayService);
  areaUnits = TOOL_AREA_UNITS;

  loanAmount = 5000000;
  loanRate = 12;
  loanYears = 20;

  buildAreaSqFt = 2250;
  buildRatePerSqFt = 3500;

  convertValue = 1;
  convertFrom: ToolAreaUnit = 'Marla';
  convertTo: ToolAreaUnit = 'Sq Ft';

  get emi(): number { return monthlyInstallment(this.loanAmount, this.loanRate, this.loanYears); }
  get totalPayable(): number { return this.emi * this.loanYears * 12; }
  get totalInterest(): number { return this.totalPayable - this.loanAmount; }
  get buildCost(): number { return constructionCost(this.buildAreaSqFt, this.buildRatePerSqFt); }
  get converted(): number { return convertArea(this.convertValue, this.convertFrom, this.convertTo); }
}
