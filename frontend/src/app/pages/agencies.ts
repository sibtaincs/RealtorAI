import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ContentService } from '../core/content.service';
import { Agency } from '../core/directory.data';
import { MasterDataService } from '../core/master-data.service';

@Component({
  selector: 'app-agencies',
  imports: [FormsModule],
  templateUrl: './agencies.html',
  styleUrl: './directory.scss'
})
export class AgenciesPage implements OnInit {
  private content = inject(ContentService);
  master = inject(MasterDataService);
  agencies = signal<Agency[]>([]);
  city = signal('');
  propertyType = signal('');

  ngOnInit(): void {
    this.content.list<Agency>('agency').subscribe(items => this.agencies.set(items));
  }

  filtered = computed(() => this.agencies().filter(a =>
    (!this.city() || a.city === this.city()) &&
    (!this.propertyType() || a.specialty.toLowerCase().includes(this.propertyType().toLowerCase()))
  ));
}
