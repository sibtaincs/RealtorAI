import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService } from '../core/content.service';
import { Agency } from '../core/directory.data';
import { CITIES } from '../core/models';

@Component({
  selector: 'app-agencies',
  imports: [FormsModule],
  templateUrl: './agencies.html',
  styleUrl: './directory.scss'
})
export class AgenciesPage implements OnInit {
  private content = inject(ContentService);
  cities = CITIES;
  agencies = signal<Agency[]>([]);
  city = signal('');
  propertyTypes = ['Residential', 'Plots', 'Apartments', 'Commercial', 'Houses', 'Rentals', 'Farms'];
  propertyType = signal('');

  ngOnInit(): void {
    this.content.list<Agency>('agency').subscribe(items => this.agencies.set(items));
  }

  filtered = computed(() => this.agencies().filter(a =>
    (!this.city() || a.city === this.city()) &&
    (!this.propertyType() || a.specialty.toLowerCase().includes(this.propertyType().toLowerCase()))
  ));
}
