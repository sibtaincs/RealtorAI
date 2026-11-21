import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AGENCIES } from '../core/directory.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-agencies',
  imports: [FormsModule],
  templateUrl: './agencies.html',
  styleUrl: './directory.scss'
})
export class AgenciesPage {
  cities = CITIES;
  agencies = AGENCIES;
  city = signal('');

  filtered = computed(() => this.agencies.filter(a => !this.city() || a.city === this.city()));
}
